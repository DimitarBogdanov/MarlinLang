using Marlin.Parsing;
using System.Collections.Generic;

namespace Marlin.Optimisation
{
    class MarlinCodeOptimiser
    {
        public readonly List<CompilerWarning> warnings;
        public static long optimisationTime = 0;

        private readonly Node rootNode;
        private readonly string file;

        public MarlinCodeOptimiser(Node rootNode, string file)
        {
            this.rootNode = rootNode;
            this.file = file;
            warnings = new();
        }

        public Node Optimise()
        {
            long start = Utils.CurrentTimeMillis();

            // TODO: Code optimisation
            
            // Benchmarking
            if (file != "") // TODO: remove, this is a quick hack to get the compiler to STFU
                optimisationTime += Utils.CurrentTimeMillis() - start;

            // We're done
            return rootNode;
        }
    }
}
