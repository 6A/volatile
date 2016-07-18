/*
______ _     _   _ _____ _   _ ___________  ___  ______  _____ ___________ 
|  ___| |   | | | |  ___| \ | |_   _| ___ \/ _ \ | ___ \/  ___|  ___| ___ \
| |_  | |   | | | | |__ |  \| | | | | |_/ / /_\ \| |_/ /\ `--.| |__ | |_/ /
|  _| | |   | | | |  __|| . ` | | | |  __/|  _  ||    /  `--. \  __||    / 
| |   | |___| |_| | |___| |\  | | | | |   | | | || |\ \ /\__/ / |___| |\ \ 
\_|   \_____/\___/\____/\_| \_/ \_/ \_|   \_| |_/\_| \_|\____/\____/\_| \_|



A single file to parse arguments easily.
Parses IEnumerable<T> and T, with T : string / int / bool

UNLICENSED: <http://unlicense.org/UNLICENSE>
*/

/*
Usage:

*/

namespace Jee
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public sealed class FluentParser<T> where T : new()
    {
        internal List<FPB> parsers;
        internal Dictionary<string, string> values;

        public string[] Args { get; private set; }

        private T item;
        public T Item
        {
            get
            {
                parse();
                return item;
            }
        }

        public static string[] Split(string s)
        {
            List<string> args = new List<string>();

            int quote = 0; // 0 (no quote), 1 (single), 2 (double)
            bool escaped = false; // escaped by a \\
            string arg = ""; // arg currently being processed

            foreach (char c in s)
            {
                if (c == '\\') // next char is escaped
                    escaped = true;
                else if (escaped) // char is escaped, move on
                {
                    escaped = false;
                    arg += c;
                }
                else // char is not escaped, process it
                {
                    if (c == ' ' && quote == 0) // separator
                    {
                        args.Add(arg);
                        arg = "";
                    }
                    else if (c == '\'') // new quote
                    {
                        if (quote == 0) // enter quote
                        {
                            quote = 1;
                        }
                        else if (quote == 1) // leave quote
                        {
                            args.Add(arg);
                            arg = "";
                        }
                        else // in another quote, add char
                        {
                            arg += c;
                        }
                    }
                    else if (c == '"') // new quote
                    {
                        if (quote == 0) // enter quote
                        {
                            quote = 2;
                        }
                        else if (quote == 2) // leave quote
                        {
                            args.Add(arg);
                            arg = "";
                        }
                        else // in another quote, add char
                        {
                            arg += c;
                        }
                    }
                    else
                    {
                        arg += c;
                    }
                }
            }

            if (arg != null) // end of string, process last arg
                args.Add(arg);

            // only take non-null args
            return args.Where(x => !String.IsNullOrWhiteSpace(x)).ToArray();
        }

        /// <summary>
        /// Parse a string into args, and create a new FluentParser out of it.
        /// </summary>
        public static FluentParser<T> FromString(string s)
        {
            return new FluentParser<T>(Split(s));
        }

        public FluentParser(string[] args)
        {
            item = new T();
            Args = args;

            parsers = new List<FPB>();
            values = new Dictionary<string, string>();
        }

        #region Parse
        private bool isPropertyKey(string arg)
        {
            return (arg.StartsWith("--") && arg.Length > 2) || (arg.StartsWith("-") && arg.Length == 2);
        }

        private void parse()
        {
            string main = String.Join(" ", Args.TakeWhile(x => !x.StartsWith("-")));

            for (int i = 0; i < Args.Length; i++)
            {
                string arg = Args[i];

                if (isPropertyKey(arg)) // long & short name
                {
                    foreach (FPB fpb in parsers.Where(x => (arg.Length == 2 && x.canParse(arg[1])) || x.canParse(arg.Substring(2))))
                    {
                        // parse values depending on the type accepted by fpb
                        if (fpb.parses == typeof(bool))
                        {
                            fpb.set(!arg.StartsWith("no-"));
                            break;
                        }
                        else if (fpb.parses == typeof(string))
                        {
                            fpb.set(Args.Length > ++i && isPropertyKey(Args[i]) ? null : Args[i]);
                            break;
                        }
                        else if (fpb.parses == typeof(int))
                        {
                            int val;
                            if (Args.Length > ++i && int.TryParse(Args[i], out val))
                            {
                                fpb.set(val);
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if (fpb.parses == typeof(string[]))
                        {
                            List<string> s = new List<string>();

                            while (Args.Length > ++i && !isPropertyKey(Args[i]))
                                s.Add(Args[i]);
                            i--;

                            fpb.set(s.ToArray());
                            break;
                        }
                        else if (fpb.parses == typeof(int[]))
                        {
                            List<int> s = new List<int>();

                            bool success = true;
                            while (Args.Length > ++i && !isPropertyKey(Args[i]))
                            {
                                int val;
                                if (int.TryParse(Args[i], out val))
                                {
                                    s.Add(val);
                                }
                                else
                                {
                                    success = false;
                                    break;
                                }
                                i--;
                            }

                            if (success)
                            {
                                fpb.set(s.ToArray());
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            throw new NotSupportedException("Cast to " + fpb.parses.Name + " not supported.");
                        }
                    }
                }
            }

            if (main != "")
            {
                foreach (FPB fpb in parsers.Where(x => x.canParseMain()))
                {
                    // parse values depending on the type accepted by fpb
                    if (fpb.parses == typeof(string))
                    {
                        fpb.set(main);
                        break;
                    }
                    else
                    {
                        throw new NotSupportedException("Cast to " + fpb.parses.Name + " not supported.");
                    }
                }
            }
        }
        #endregion

        internal void Set(MemberExpression ex, object value)
        {
            if (ex.Member is FieldInfo)
                (ex.Member as FieldInfo).SetValue(item, value);
            else if (ex.Member is PropertyInfo)
                (ex.Member as PropertyInfo).SetValue(item, value, null);
            else
                throw new ArgumentException();
        }

        public FluentParserBuilder<TReturn> Define<TReturn>(Expression<Func<T, TReturn>> expr)
        {
            return new FluentParserBuilder<TReturn>(this, parsers.Count, expr.Body as MemberExpression);
        }

        #region Builder abstract class
        public abstract class FPB
        {
            internal abstract Type parses { get; set; }

            internal abstract bool canParseMain();
            internal abstract bool canParse(string name);
            internal abstract bool canParse(char name);
            internal abstract void set(object value);
            internal abstract void setDefault();
        }
        #endregion

        #region Builder
        public class FluentParserBuilder<TReturn> : FPB
        {
            private FluentParser<T> _fp;
            private int _id;
            private MemberExpression _ex;

            private string[] longs = new string[0];
            private char[] shorts = new char[0];
            
            private TReturn def = default(TReturn);
            private bool defSet = false;
            private bool main = false;

            internal override Type parses { get; set; }

            internal FluentParserBuilder(FluentParser<T> fp, int id, MemberExpression ex)
            {
                parses = typeof(TReturn);

                _fp = fp;
                _id = id;
                _ex = ex;
            }

            internal override bool canParse(string name)
            {
                if (typeof(TReturn) == typeof(bool) && name.StartsWith("no-"))
                    return longs.Contains(name.Substring(3));
                return longs.Contains(name);
            }

            internal override bool canParse(char name)
            {
                return shorts.Contains(name);
            }

            internal override void set(object value)
            {
                _fp.Set(_ex, value);
            }

            internal override void setDefault()
            {
                if (defSet)
                    _fp.Set(_ex, def);
                else
                    _fp.Set(_ex, default(TReturn));
            }

            internal override bool canParseMain()
            {
                return main;
            }

            private FluentParserBuilder<TReturn> update()
            {
                if (_fp.parsers.Count > _id)
                    _fp.parsers[_id] = this;
                else
                    _fp.parsers.Add(this);
                return this;
            }

            public FluentParserBuilder<TReturn> Long(params string[] names)
            {
                longs = names;
                return update();
            }

            public FluentParserBuilder<TReturn> Short(params char[] names)
            {
                shorts = names;
                return update();
            }

            public FluentParserBuilder<TReturn> Default(TReturn value)
            {
                def = value;
                defSet = true;
                return update();
            }

            public FluentParserBuilder<TReturn> Main()
            {
                main = true;
                return update();
            }

            public FluentParser<T> Done()
            {
                return _fp;
            }
        }
        #endregion
    }
}