using System.Collections.Generic;

namespace CallingBotSample.Configuration
{
    public class Offices
    {
        public List<Office> offices { get; set; }
    }

    /// <summary>
    /// COSMOS DB (Our DB Table include Table Name : tbl_offices)
    /// </summary>
    public class Office
    {
        public string Id { get; set; }
        public string Name { get; set; } //Office Name
        public string Address { get; set; }
        public string TelephoneNumber { get; set; }
        public string GreetingCopy { get; set; }
    }

}
