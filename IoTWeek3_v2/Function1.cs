using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using IoTWeek3_v2.models;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;

namespace IoTWeek3_v2
{
    public static class Function1
    {
        //SQL SERVER

        [FunctionName("AddRegistration")]
        public static async Task<IActionResult> AddRegistration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/registration")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var json = await new StreamReader(req.Body).ReadToEndAsync();
                var registration = JsonConvert.DeserializeObject<Registration>(json);

                string guid = Guid.NewGuid().ToString();
                registration.RegistrationId = guid;

                //connectie maken via connectiestring
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLConnectionString")))
                {
                    await connection.OpenAsync();

                    //sql commando opstellen
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Connection = connection;
                        //sql statement opstellen
                        command.CommandText = "INSERT INTO Registrations VALUES (@RegistrationId, @FirstName, @LastName, @EMail, @Zipcode, @Age, @IsFirstTimer)";
                        command.Parameters.AddWithValue("@RegistrationId", registration.RegistrationId);
                        command.Parameters.AddWithValue("@FirstName", registration.Firstname);
                        command.Parameters.AddWithValue("@LastName", registration.Lastname);
                        command.Parameters.AddWithValue("@EMail", registration.Email);
                        command.Parameters.AddWithValue("@Zipcode", registration.Zipcode);
                        command.Parameters.AddWithValue("@Age", registration.Age);
                        command.Parameters.AddWithValue("@IsFirstTimer", registration.IsFirstTimer);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return new OkObjectResult(registration);
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return new StatusCodeResult(500);
            }
        }

        [FunctionName("GetRegistrations")]
        public static async Task<IActionResult> GetRegistrations(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/registrations")] HttpRequest req,
            ILogger log)
        {
            try
            {
                List<Registration> listRegistrations = new List<Registration>();
                //connectie maken via connectiestring
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLConnectionString")))
                {
                    await connection.OpenAsync();
                    //sql commando opstellen
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Connection = connection;
                        //sql statement opstellen
                        command.CommandText = "SELECT * FROM Registrations";
                        //data lezen en opslaan
                        SqlDataReader reader = await command.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            listRegistrations.Add(new Registration()
                            {
                                RegistrationId = reader["RegistrationId"].ToString(),
                                Firstname = reader["FirstName"].ToString(),
                                Lastname = reader["LastName"].ToString(),
                                Email = reader["EMail"].ToString(),
                                Zipcode = reader["Zipcode"].ToString(),
                                Age = int.Parse(reader["Age"].ToString()),
                                IsFirstTimer = bool.Parse(reader["IsFirstTimer"].ToString())
                            });
                        }
                    }
                }
                //lijst terugsturen met juiste statuscode
                return new OkObjectResult(listRegistrations);
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return new StatusCodeResult(500);
            }

        }

        //STORAGE

        [FunctionName("AddRegistrationStorage")]
        public static async Task<IActionResult> AddRegistrationStorage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v2/registration")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var json = await new StreamReader(req.Body).ReadToEndAsync();
                Registration registration = JsonConvert.DeserializeObject<Registration>(json);

                string registrationId = Guid.NewGuid().ToString();

                //object aanmaken
                RegistrationEntity registrationEntity = new RegistrationEntity(registration.Zipcode, registrationId)
                {
                    FirstName = registration.Firstname,
                    LastName = registration.Lastname,
                    EMail = registration.Email,
                    Zipcode = registration.Zipcode,
                    Age = registration.Age,
                    IsFirstTimer = registration.IsFirstTimer
                };

                //connectie maken met tabel
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));
                CloudTableClient cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
                CloudTable cloudTable = cloudTableClient.GetTableReference("registrations");
                await cloudTable.CreateIfNotExistsAsync();

                //insert
                TableOperation insert = TableOperation.Insert(registrationEntity);
                await cloudTable.ExecuteAsync(insert);

                return new OkObjectResult(registration);
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return new StatusCodeResult(500);
            }
        }

        [FunctionName("GetRegistrationsStorage")]
        public static async Task<IActionResult> GetRegistrationsStorage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v2/registrations/{zipcode}")] HttpRequest req, string zipcode,
            ILogger log)
        {
            try
            {
                //connectie maken met tabel
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));
                CloudTableClient cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
                CloudTable cloudTable = cloudTableClient.GetTableReference("registrations");

                TableQuery<RegistrationEntity> rangeQuery = new TableQuery<RegistrationEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, zipcode));

                //lege ctor maken in RegistrationEntity!!
                var queryResult = await cloudTable.ExecuteQuerySegmentedAsync<RegistrationEntity>(rangeQuery, null);
                List<Registration> listRegistrations = new List<Registration>();

                foreach (var reg in queryResult.Results)
                {
                    listRegistrations.Add(new Registration()
                    {
                        Firstname = reg.FirstName,
                        Lastname = reg.LastName,
                        Email = reg.EMail,
                        Zipcode = reg.Zipcode,
                        Age = reg.Age,
                        IsFirstTimer = reg.IsFirstTimer
                    });
                }
                return new OkObjectResult(listRegistrations);
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return new StatusCodeResult(500);
            }
        }
    }
}
