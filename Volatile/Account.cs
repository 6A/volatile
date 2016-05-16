using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace Volatile
{
    [VolatileObject]
    class Account
    {
        [VolatileObjectMember(0)]
        public string Password { get; set; }

        [VolatileObjectMember(1)]
        public string Email { get; set; }

        [VolatileObjectMember(2)]
        public string Username { get; set; }

        [VolatileObjectMember(3)]
        public string Website { get; set; }

        [VolatileObjectMember(4)]
        public string Hint { get; set; }

        public Account(string pass, string email, string user, string website, string hint)
        {
            Password = pass;
            Email = email;
            Username = user;
            Website = website;
            Hint = hint;
        }
    }
}
