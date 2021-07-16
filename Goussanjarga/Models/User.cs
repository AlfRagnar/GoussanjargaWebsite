using Newtonsoft.Json;
using System;

namespace Goussanjarga.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string UserPrincipalName { get; set; }
        public string MobilePhone { get; set; }
        public string Mail { get; set; }
    }
}