using System;

namespace RdtClient.Service.Models
{
    public class Profile
    {
        public String UserName { get; set; }
        public DateTimeOffset Expiration { get; set; }
    }
}
