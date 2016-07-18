namespace Volatile
{
    using System;
    using System.Linq;
    using Jee;
    using System.Text;
    using System.IO;

    static class Program
    {
        static VolatileContext<Account> Accounts;
        static bool IsVerbose;

        /// <summary>
        /// Options parsed when starting the app
        /// </summary>
        class ArgOptions : Options
        {
            public string FilePassword;
            public bool Remove;
            public bool Export;

            public bool Verbose;
        }

        /// <summary>
        /// Options parsed in interactive mode
        /// </summary>
        class Options
        {
            public string Password;
            public string Username;
            public string Email;
            public string Hint;
            public string Website;
        }

        static void Log(string str)
        {
            if (IsVerbose)
                Console.WriteLine(str);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // parse arguments ; if they're given, simply react to them and move on
            ArgOptions argv = new FluentParser<ArgOptions>(Environment.GetCommandLineArgs().Skip(1).ToArray())
                // pass password
                .Define(x => x.FilePassword).Main().Done()

                // pass options (remove, export)
                .Define(x => x.Remove).Long("remove", "delete").Short('r').Done()
                .Define(x => x.Export).Long("export").Short('x').Done()
                .Define(x => x.Verbose).Long("verbose").Short('v').Done()

                // pass options (website, website password, etc)
                .Define(x => x.Website).Long("website").Short('w').Done()
                .Define(x => x.Password).Long("password", "pass").Short('p').Done()
                .Define(x => x.Username).Long("username", "user").Short('u').Done()
                .Define(x => x.Email).Long("email", "mail").Short('e').Done()
                .Define(x => x.Hint).Long("hint").Done()

                // parse
                .Item;

            string pass = argv.FilePassword;

            if (pass == null) // no password given? ask for it
            {
                Console.Write("Please enter a password: ");
                pass = Console.ReadLine();
            }

            if (TryUnlock(pass)) // right password?
            {
                Console.Clear();
                
                if (argv.Website != null) // if args given...
                {
                    if (argv.Remove) // remove website
                    {
                        React($"remove {argv.Website}".Split(' '));
                    }
                    else if (argv.Username != null && argv.Email != null) // multiple args ; add / update a website
                    {
                        StringBuilder sb = new StringBuilder($"set {argv.Website}");

                        if (argv.Email != null) sb.Append($" -e {argv.Email}");
                        if (argv.Username != null) sb.Append($" -u {argv.Username}");
                        if (argv.Hint != null) sb.Append($" -h {argv.Hint}");
                        if (argv.Password != null) sb.Append($" -p {argv.Password}");

                        React(sb.ToString().Split(' '));
                    }
                    else // only the website is given ; return the website
                    {
                        React($"get {argv.Website}".Split(' '));
                    }
                }
                else // interactive
                {
                    Console.WriteLine("Welcome.");
                    Console.WriteLine();
                    Console.WriteLine("get [site]");
                    Console.WriteLine();
                    Console.WriteLine("set [site]");
                    Console.WriteLine("    -p, --pass, --password [password]");
                    Console.WriteLine("    -u, --user, --username [username]");
                    Console.WriteLine("    -e, --mail, --email [email]");
                    Console.WriteLine("    -h, --hint [hint]");
                    Console.WriteLine();
                    Console.WriteLine("remove [site]");
                    Console.WriteLine();
                    Console.WriteLine("export [filename]");
                    Console.WriteLine();

                    IsVerbose = true;

                    string cmd;
                    while ((cmd = Console.ReadLine().Trim()) != "exit" && cmd != "shutdown" && cmd != "stop" && cmd != "quit")
                        React(FluentParser<object>.Split(cmd));
                }               

                Accounts.Dispose();
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
                Accounts = new VolatileContext<Account>(password);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Perform <see cref="string.Equals(string, string, StringComparison)"/> with
        /// <see cref="StringComparison.InvariantCultureIgnoreCase"/>.
        /// </summary>
        static bool Same(string a, string b)
        {
            return string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);
        }
        
        static void React(string[] input)
        {
            if (input.Length == 0)
                return;

            string cmd = input[0];

            if (Same(cmd, "export"))
            {
                if (input.Length > 1)
                {
                    string path = Path.GetFullPath(input[1]);
                    Log($"Saving to {path}...");
                    try
                    {
                        File.WriteAllText(path, ToJSON(), Encoding.UTF8);
                        Log($"Done.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to save to {path}: {e.Message}");
                    }
                }
                else
                {
                    Console.WriteLine(ToJSON());
                }
                return;
            }

            Options opts = new FluentParser<Options>(input.Skip(1).ToArray())
                // pass options (website, website password, etc)
                .Define(x => x.Website).Main().Done()
                .Define(x => x.Password).Long("password", "pass").Short('p').Done()
                .Define(x => x.Username).Long("username", "user").Short('u').Done()
                .Define(x => x.Email).Long("email", "mail").Short('e').Done()
                .Define(x => x.Hint).Long("hint").Done()

                // parse
                .Item;

            if (String.IsNullOrWhiteSpace(opts.Website))
            {
                Console.WriteLine("No site specified.");
                return;
            }

            if (Same(cmd, "remove") || Same(cmd, "delete"))
            {
                Account a = Accounts.FirstOrDefault(x => Same(x.Website, opts.Website));
                if (a == null)
                {
                    Console.WriteLine($"Couldn't find '{opts.Website}'.");
                }
                else
                {
                    Log($"Website '{opts.Website}' removed.");

                    Accounts.Remove(a);
                }
            }
            else if (Same(cmd, "read") || Same(cmd, "get"))
            {
                Account a = Accounts.FirstOrDefault(x => Same(x.Website, opts.Website));
                if (a == null)
                {
                    Console.WriteLine($"Couldn't find '{opts.Website}'.");
                }
                else
                {
                    Console.Write(a.ToString());
                }
            }
            else if (Same(cmd, "write") || Same(cmd, "set"))
            {
                Account a = Accounts.FirstOrDefault(x => Same(x.Website, opts.Website));

                if (a == null)
                    a = new Account { Website = opts.Website };
                else
                    Accounts.Remove(a);

                if (opts.Username != null)
                    a.Username = opts.Username;
                if (opts.Password != null)
                    a.Password = opts.Password;
                if (opts.Email != null)
                    a.Email = opts.Email;
                if (opts.Hint != null)
                    a.Hint = opts.Hint;

                Accounts.Add(a);
                Log("Account added to database.");
            }
            else
            {
                Console.WriteLine("Unknown command.");
            }
        }

        static string ToJSON()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('{');
            sb.Append('\n');

            int i = 0;
            foreach (Account a in Accounts)
            {
                sb.AppendFormat("  \"{0}\": {{\"username\": {1}, \"password\": {2}, \"email\": {3}, \"hint\": {4}}}{5}\n",
                    a.Website,
                    a.Username == null ? $"\"{a.Username}\"" : "null",
                    a.Password == null ? $"\"{a.Password}\"" : "null",
                    a.Email == null ? $"\"{a.Email}\"" : "null",
                    a.Hint == null ? $"\"{a.Hint}\"" : "null",
                    i < Accounts.Count - 1 ? "," : String.Empty);
            }

            sb.Append('}');
            return sb.ToString();
        }
    }
}
