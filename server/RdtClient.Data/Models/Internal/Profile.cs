using System;

namespace RdtClient.Data.Models.Internal
{
    public class Profile
    {
        public String UserName { get; set; }
        public DateTimeOffset? Expiration { get; set; }
    }
}
