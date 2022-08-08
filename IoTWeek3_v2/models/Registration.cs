using System;
using System.Collections.Generic;
using System.Text;

namespace IoTWeek3_v2.models
{
    public class Registration
    {
        public string RegistrationId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Zipcode { get; set; }
        public int Age { get; set; }
        public bool IsFirstTimer { get; set; }
    }
}
