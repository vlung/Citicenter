using System;
using System.Collections.Generic;
using System.Text;

namespace TP
{
    public class CommandLineParser
    {
        private class CommandLineArgument
        {
            string _arg;
            string _name;
            string _desc;
            string _defVal;
            string _value = null;

            public CommandLineArgument(string arg, string name, string desc) : this(arg, name, desc, null) { }

            public CommandLineArgument(string arg, string name, string desc, string defVal)
            {
                _arg = arg;
                _name = name;
                _desc = desc;
                _defVal = defVal;
                _value = null;
            }

            public string Value
            {
                get
                {
                    if (_value == null)
                    {
                        return _defVal;
                    }
                    else
                    {
                        return _value;
                    }
                }
                set
                {
                    if (_value == null)
                    {
                        _value = value;
                    }
                }
            }

            public override string ToString()
            {
                return base.ToString();
            }

            public string ToShortString()
            {
                if (_defVal == null)
                {
                    return string.Format("/{0} <{1}>", _arg, _name);
                }
                else
                {
                    return string.Format("[/{0} <{1}>]", _arg, _name);
                }
            }

            public string ToLongString()
            {
                if (_defVal == null)
                {
                    return string.Format("/{0}\n{1}",
                                         _arg, _desc);
                }
                else
                {
                    return string.Format("[/{0}]\n" +
                                         "{1}\n" +
                                         "(Default: {2})",
                                         _arg,
                                         _desc,
                                         _defVal);
                }
            }
        }

        Dictionary<string, CommandLineArgument> args = new Dictionary<string, CommandLineArgument>();

        public void Add(string arg, string name, string desc)
        {
            args[arg.ToLower()] = new CommandLineArgument(arg, name, desc);
        }

        public void Add(string arg, string name, string desc, string defVal)
        {
            args[arg.ToLower()] = new CommandLineArgument(arg, name, desc, defVal);
        }

        public string this[string arg]
        {
            get
            {
                arg = arg.ToLower();

                if (args.ContainsKey(arg))
                {
                    return args[arg].Value;
                }
                else
                {
                    return null;
                }
            }
        }

        public bool Parse(string[] clargs)
        {
            bool success = (clargs.Length % 2) == 0;

            if (success)
            {
                for (int i = 0; i < clargs.Length && success; i += 2)
                {
                    if (clargs[i].StartsWith("-") || clargs[i].StartsWith("/"))
                    {
                        string tempArg = clargs[i].Substring(1).ToLower();
                        if (args.ContainsKey(tempArg))
                        {
                            args[tempArg].Value = clargs[i + 1];
                        }
                        else
                        {
                            success = false;
                        }
                    }
                    else
                    {
                        success = false;
                    }
                }
            }

            if (success)
            {
                foreach (CommandLineArgument arg in args.Values)
                {
                    if (arg.Value == null)
                    {
                        success = false;
                        break;
                    }
                }
            }

            if (!success)
            {
                Help();
            }

            return success;
        }

        public void Help()
        {
            Console.Write(System.Diagnostics.Process.GetCurrentProcess().ProcessName);

            foreach (CommandLineArgument arg in args.Values)
            {
                Console.Write(" {0}", arg.ToShortString());
            }

            Console.WriteLine();
            Console.WriteLine();

            foreach (CommandLineArgument arg in args.Values)
            {
                Console.WriteLine(arg.ToLongString());
                Console.WriteLine();
            }
        }
    }
}
