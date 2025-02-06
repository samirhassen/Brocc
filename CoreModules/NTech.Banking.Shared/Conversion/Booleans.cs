using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech
{
    public static class Booleans
    {
        public static bool ExactlyOneIsTrue(params bool[] args)
        {
            if (args == null || args.Length == 0)
                return false;
            return args.Where(x => x).Count() == 1;
        }
    }
}