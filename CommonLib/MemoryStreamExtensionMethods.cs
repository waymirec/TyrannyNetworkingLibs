using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tyranny.Networking
{
    public static class MemoryStreamExtensionMethods
    {
        public static void Clear(this MemoryStream ms)
        {
            byte[] buffer = ms.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);
            ms.Position = 0;
            ms.SetLength(0);
        }
    }
}
