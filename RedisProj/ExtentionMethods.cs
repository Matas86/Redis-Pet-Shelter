using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Extentions
{
    public static class ExtentionMethods
    {
        static readonly string[] nix = new string[0];
        /// <summary>
        /// Create an array of strings from an array of values
        /// </summary>
        public static string[] ToStringArray(this RedisValue[] values)
        {
            if (values == null) return null;
            if (values.Length == 0) return nix;
            return Array.ConvertAll(values, x => (string)x);
        }
    }
}
