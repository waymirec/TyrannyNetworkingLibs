using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLib
{
    public static class GuidExtensionMethods
    {
        public static string Normalized(this Guid guid)
        {
            return guid.ToString().Replace("-", "");
        }
    }
}
