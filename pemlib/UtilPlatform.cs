using System;
using System.Runtime;
using System.Collections.Generic;
using System.Globalization;

namespace Org.BouncyCastle.Utilities
{
    internal abstract class Platform
    {
        private static readonly CompareInfo InvariantCompareInfo = CultureInfo.InvariantCulture.CompareInfo;

        private static string GetNewLine()
        {
            return Environment.NewLine;
        }

        internal static bool EqualsIgnoreCase(string a, string b)
        { return String.Equals(a, b, StringComparison.OrdinalIgnoreCase); }

        internal static string GetEnvironmentVariable(string variable)
        {
            try
            {
                return Environment.GetEnvironmentVariable(variable);
            }
            catch (System.Security.SecurityException)
            {
                // We don't have the required permission to read this environment variable,
                // which is fine, just act as if it's not set
                return null;
            }
        }

        internal static Exception CreateNotImplementedException(string message)
        {
            return new NotImplementedException(message);
        }

        internal static System.Collections.IList CreateArrayList()
        {
            return new List<object>();
        }
        internal static System.Collections.IList CreateArrayList(int capacity)
        {
            return new List<object>(capacity);
        }
        internal static System.Collections.IList CreateArrayList(System.Collections.ICollection collection)
        {
            System.Collections.IList result = new List<object>(collection.Count);
            foreach (object o in collection)
            {
                result.Add(o);
            }
            return result;
        }
        internal static System.Collections.IList CreateArrayList(System.Collections.IEnumerable collection)
        {
            System.Collections.IList result = new List<object>();
            foreach (object o in collection)
            {
                result.Add(o);
            }
            return result;
        }
        internal static System.Collections.IDictionary CreateHashtable()
        {
            return new Dictionary<object, object>();
        }
        internal static System.Collections.IDictionary CreateHashtable(int capacity)
        {
            return new Dictionary<object, object>(capacity);
        }
        internal static System.Collections.IDictionary CreateHashtable(System.Collections.IDictionary dictionary)
        {
            System.Collections.IDictionary result = new Dictionary<object, object>(dictionary.Count);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                result.Add(entry.Key, entry.Value);
            }
            return result;
        }

        internal static string ToLowerInvariant(string s)
        {
            return s.ToLowerInvariant();
        }

        internal static string ToUpperInvariant(string s)
        {
            return s.ToUpperInvariant();
        }

        internal static readonly string NewLine = GetNewLine();

        internal static void Dispose(IDisposable d)
        {
            d.Dispose();
        }

        internal static int IndexOf(string source, string value)
        {
            return InvariantCompareInfo.IndexOf(source, value, CompareOptions.Ordinal);
        }

        internal static int LastIndexOf(string source, string value)
        {
            return InvariantCompareInfo.LastIndexOf(source, value, CompareOptions.Ordinal);
        }

        internal static bool StartsWith(string source, string prefix)
        {
            return InvariantCompareInfo.IsPrefix(source, prefix, CompareOptions.Ordinal);
        }

        internal static bool EndsWith(string source, string suffix)
        {
            return InvariantCompareInfo.IsSuffix(source, suffix, CompareOptions.Ordinal);
        }

        internal static string GetTypeName(object obj) { return obj.GetType().FullName; }
    }
}