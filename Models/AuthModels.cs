using System;
using System.Collections.Generic;

namespace GLC_EXPRESS.Models
{
    public class AuthUserRecord
    {
        public AuthUserRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            Roles = new List<string>();
            CreatedAtUtc = DateTime.UtcNow;
            IsActive = true;
        }

        public string Id { get; set; }

        public string Username { get; set; }

        public string UsernameNormalized { get; set; }

        public string PasswordHash { get; set; }

        public List<string> Roles { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
