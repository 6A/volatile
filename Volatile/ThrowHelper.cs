using System;
using System.Collections.Generic;
using System.Text;

namespace Volatile
{
    static class ThrowHelper
    {
        public const string NOTFOUND = @"Password not found.";
        public const string IOERROR = @"Couldn't write to file.";
        public const string TOOBIG = @"Too many passwords saved.";

        public static void Throw(string msg)
        {
#if DEBUG
            throw new Exception(msg);
#endif
        }

        public static T Throw<T>(string msg)
        {
            Throw(msg);
            return default(T);
        }
    }
}
