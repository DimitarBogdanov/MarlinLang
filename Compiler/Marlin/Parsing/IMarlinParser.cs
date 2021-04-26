/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     Tokenizer.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

namespace Marlin.Parsing
{
    interface IMarlinParser
    {
        public Node Parse();
    }
}
