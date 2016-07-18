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

        public Account()
        {
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(Username))
                sb.AppendLine($"  Username: {Username}");

            if (!String.IsNullOrWhiteSpace(Password))
                sb.AppendLine($"  Password: {Password}");

            if (!String.IsNullOrWhiteSpace(Email))
                sb.AppendLine($"  Email address: {Email}");

            if (!String.IsNullOrWhiteSpace(Hint))
                sb.AppendLine($"  Hint: {Hint}");

            return sb.Length == 0 ? "  [empty]\n" : sb.ToString();
        }
    }
}
