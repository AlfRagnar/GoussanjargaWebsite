using Newtonsoft.Json;
using System.ComponentModel;

namespace Goussanjarga.Models
{
    public class Families
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [DisplayName("Family Last Name")]
        public string LastName { get; set; }

        [DisplayName("Parents")]
        public Parent[] Parents { get; set; }

        [DisplayName("Children")]
        public Child[] Children { get; set; }

        [DisplayName("Family Address")]
        public Address Address { get; set; }

        [DisplayName("Registered?")]
        public bool IsRegistered { get; set; }

        // The ToString() method is used to format the output, it's used for demo purpose only. It's not required by Azure Cosmos DB
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Parent
    {
        [DisplayName("Family Name")]
        public string FamilyName { get; set; }

        [DisplayName("First Name")]
        public string FirstName { get; set; }
    }

    public class Child
    {
        [DisplayName("Family Name")]
        public string FamilyName { get; set; }

        [DisplayName("First Name")]
        public string FirstName { get; set; }

        [DisplayName("Gender")]
        public string Gender { get; set; }

        public int Grade { get; set; }

        [DisplayName("Pets")]
        public Pet[] Pets { get; set; }
    }

    public class Pet
    {
        [DisplayName("Pets Name")]
        public string GivenName { get; set; }
    }

    public class Address
    {
        public string State { get; set; }
        public string County { get; set; }
        public string City { get; set; }
    }
}