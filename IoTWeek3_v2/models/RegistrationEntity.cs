using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace IoTWeek3_v2.models
{
    public class RegistrationEntity : TableEntity
    {
        public RegistrationEntity(string zipcode, string registrationId)
        {
            PartitionKey = zipcode; //PK: records met dezelfde PK groeperen
            RowKey = registrationId;//RK: GUID nemen
        }

        public RegistrationEntity()
        {

        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EMail { get; set; }
        public string Zipcode { get; set; }
        public int Age { get; set; }
        public bool IsFirstTimer { get; set; }
    }
}
