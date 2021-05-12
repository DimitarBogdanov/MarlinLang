using Marlin.Parsing;
using System;

namespace Marlin.CodeGen
{
    public class CLITarget : Target
    {
        public override void BeginTranslation(Node ast)
        {
            throw new NotImplementedException("The CLI target is not yet implemented. Please use LLVM for the time being.");
        }
    }
}
