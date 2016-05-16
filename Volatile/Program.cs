using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Volatile
{
    static class Program
    {
        static VolatileContext<Account> accounts;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string args = String.Join(" ", Environment.GetCommandLineArgs());

            Match m = Regex.Match(args, @"(?<=(?:-p|--pass|--password)(?:[ :=]))\S+");

            string pass;
            if (m.Success)
            {
                pass = m.Value;
            }
            else
            {
                Console.Write("Please enter a password: ");
                pass = Console.ReadLine();
            }

            if (TryUnlock(pass))
            {
                Console.Clear();
                Console.WriteLine("Welcome.");
                Console.WriteLine();
                Console.WriteLine("get [site]");
                Console.WriteLine();
                Console.WriteLine("add [site]");
                Console.WriteLine("    -p, --pass, --password [password]");
                Console.WriteLine("    -u, --user, --username [username]");
                Console.WriteLine("    -@, --mail, --email [email]");
                Console.WriteLine("    -h, --hint [hint]");
                Console.WriteLine();
                Console.WriteLine("remove [site]");
                Console.WriteLine();
                Console.WriteLine("update [site]");
                Console.WriteLine("    -p, --pass, --password [password]");
                Console.WriteLine("    -u, --user, --username [username]");
                Console.WriteLine("    -@, --mail, --email [email]");
                Console.WriteLine("    -h, --hint [hint]");
                Console.WriteLine();
                Console.WriteLine("export [filename]");
                Console.WriteLine();

                string cmd;
                while ((cmd = Console.ReadLine().Trim()) != "exit" && cmd != "shutdown" && cmd != "stop" && cmd != "quit")
                    React(cmd);

                accounts.Dispose();
            }
            else
            {
                Console.WriteLine("Invalid password, please try again.");
                Console.ReadKey();
            }
        }

        static bool TryUnlock(string password)
        {
            try
            {
                accounts = new VolatileContext<Account>(password);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        const string rgxParser = @"((?:-p|--pass|--password)[ :=])(?<p>\""[^""]*\""|\S+)|((?:-u|--user|--username)[ :=])(?<u>\""[^""] *\""|\S+)|((?:-@|--mail|--email)[ :=])(?<e>\""[^""]*\""|\S+)|((?:-h|--hint)[ :=])(?<h>\""[^""]*\""|\S+)";
        static void React(string input)
        {
            string site = Regex.Match(input, @"(?<=\S+ )\S+").Value;
            if (String.IsNullOrEmpty(site))
            {
                Console.WriteLine("No site specified.");
            }

            if (input.StartsWith("remove ") || input.StartsWith("delete "))
            {
                site = site.ToLower();

                Account a = accounts.FirstOrDefault(x => x.Website.ToLower() == site);
                if (a == null)
                {
                    Console.WriteLine("Couldn't find '" + site + "'.");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Website '{0}' removed.", a.Website);

                    accounts.Remove(a);
                }
            }
            else if (input.StartsWith("read ") || input.StartsWith("get "))
            {
                site = site.ToLower();

                Account a = accounts.FirstOrDefault(x => x.Website.ToLower() == site);
                if (a == null)
                {
                    Console.WriteLine("Couldn't find '" + site + "'.");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("WEBSITE: {0}\n  USERNAME: {1}\n  PASSWORD: {2}\n  EMAIL ADDRESS: {3}\n  HINT: {4}",
                        a.Website, a.Username, a.Password, a.Email, a.Hint);
                }
            }
            else if (input.StartsWith("write ") || input.StartsWith("add ") || input.StartsWith("post "))
            {
                if (accounts.Any(x => x.Website.ToLower() == site.ToLower()))
                {
                    Console.WriteLine("Site '" + site + "' already exists.");
                }
                else
                {
                    Account a = new Account(null, null, null, site, null);
                    var m = Regex.Matches(input, rgxParser, RegexOptions.IgnoreCase).Cast<Match>();

                    if (m.Any(x => x.Groups["p"].Success))
                        a.Password = m.First(x => x.Groups["p"].Success).Groups["p"].Value;

                    if (m.Any(x => x.Groups["u"].Success))
                        a.Username = m.First(x => x.Groups["u"].Success).Groups["u"].Value;

                    if (m.Any(x => x.Groups["e"].Success))
                        a.Email = m.First(x => x.Groups["e"].Success).Groups["e"].Value;

                    if (m.Any(x => x.Groups["h"].Success))
                        a.Hint = m.First(x => x.Groups["h"].Success).Groups["h"].Value;

                    accounts.Add(a);
                    Console.WriteLine("Account added to database.");
                }
            }
            else if (input.StartsWith("update ") || input.StartsWith("put "))
            {
                site = site.ToLower();

                Account a = accounts.FirstOrDefault(x => x.Website.ToLower() == site);
                if (a == null)
                {
                    Console.WriteLine("Couldn't find '" + site + "'.");
                }
                else
                {
                    accounts.Remove(a);

                    var m = Regex.Matches(input, rgxParser, RegexOptions.IgnoreCase).Cast<Match>();

                    if (m.Any(x => x.Groups["p"].Success))
                        a.Password = m.First(x => x.Groups["p"].Success).Groups["p"].Value;

                    if (m.Any(x => x.Groups["u"].Success))
                        a.Username = m.First(x => x.Groups["u"].Success).Groups["u"].Value;

                    if (m.Any(x => x.Groups["e"].Success))
                        a.Email = m.First(x => x.Groups["e"].Success).Groups["e"].Value;

                    if (m.Any(x => x.Groups["h"].Success))
                        a.Hint = m.First(x => x.Groups["h"].Success).Groups["h"].Value;

                    accounts.Add(a);
                    Console.WriteLine("Account updated.");
                }
            }
            else
            {
                Console.WriteLine("Unknown command.");
            }
        }
    }
}
