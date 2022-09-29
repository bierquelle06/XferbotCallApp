using System.Collections.Generic;

namespace CallingBotSample.Configuration
{
    public class Users
    {
        public List<User> users { get; set; }
    }

    /// <summary>
    /// GRAPH USER (https://portal.office.com/adminportal/home?#/users)
    /// </summary>
    public class User
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string OfficeLocation { get; set; } //User Location => Office Name
        public string MobilePhone { get; set; }
        public string Language { get; set; }
    }

}
