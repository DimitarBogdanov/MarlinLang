﻿/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     CommandOptions.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

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
            option = option.ToLower();
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
            option = option.ToLower();
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
