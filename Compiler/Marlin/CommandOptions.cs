using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marlin
{
    public class CommandOptions
    {
        private readonly List<string> arguments;

        public CommandOptions(string[] args)
        {
            arguments = args.ToList();
        }

        public bool HasOption(string option, bool requireValue = false)
        {
            foreach (string str in arguments)
            {
                if (str.ToLower() == option)
                {
                    if (requireValue)
                    {
                        return GetOption(option) != "";
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public string GetOption(string option)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                if (arguments[i].ToLower() == option)
                {
                    try
                    {
                        string nextArg = arguments[i + 1];
                        return nextArg.StartsWith("--") ? "" : nextArg;
                    } catch (Exception)
                    {
                        return "";
                    }
                }
            }

            return null;
        }
    }
}
