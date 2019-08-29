using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MakeGDI
{
    public class CommandLineHelper
    {
        private static bool IsArgument(string arg)
        {
            return arg.StartsWith("-"); /* FIXME: Probably we can do better here */
        }

        private static bool CompareArguments(string arg, string shortArg, string longArg)
        {
            return arg.Equals("-" + shortArg, StringComparison.OrdinalIgnoreCase)
                || arg.Equals("--" + longArg, StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasArgument(string[] args, string shortArgument, string longArgument)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (CompareArguments(args[i], shortArgument, longArgument))
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetSoloArgument(string[] args, string shortArgument, string longArgument)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (CompareArguments(args[i], shortArgument, longArgument))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        public static List<string> GetMultiArgument(string[] args, string shortArgument, string longArgument)
        {
            List<string> retval = new List<string>();
            int start = -1;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (CompareArguments(args[i], shortArgument, longArgument))
                {
                    start = i + 1;
                    break;
                }
            }
            if (start > 0)
            {
                for (int i = start; i < args.Length; i++)
                {
                    if (IsArgument(args[i]))
                    {
                        break;
                    }
                    else
                    {
                        retval.Add(args[i]);
                    }
                }
            }
            return retval;
        }
    }
}
