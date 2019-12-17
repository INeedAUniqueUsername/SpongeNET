using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpongeNET
{
    static class Helper
    {
        public static string Try(dynamic data, string member, string fallback = "")
        {
            try
            {
                return data[member];
            } catch
            {
                return fallback;
            }
        }
        public static bool Try(dynamic data, string member, bool fallback)
        {
            try
            {
                return data[member];
            } catch
            {
                return fallback;
            }
        }
        public static int Try(dynamic data, string member, int fallback)
        {
            try
            {
                return data[member];
            }
            catch
            {
                return fallback;
            }
        }
        public static T Try<T>(dynamic data, string member, T fallback)
        {
            try
            {
                return data[member];
            } catch
            {
                return fallback;
            }
        }
        public static T TryEnum<T>(dynamic data, string member, T fallback) where T: struct
        {
            try
            {
                return Enum.Parse<T>(data[member]);
            } catch
            {
                return fallback;
            }
        }
        public static HashSet<int> Try(dynamic data, string member, HashSet<int> fallback)
        {
            try
            {
                return ((JArray)JArray.Parse(data[member])).ToObject<HashSet<int>>();
            } catch
            {
                return fallback;
            }
        }
        public static string Subsplit(ref string s, char separator = ' ') {
            int index = s.IndexOf(separator);
            if (index == -1) {
                var result = s;
                s = "";
                return result;
            } else {
                var result = s.Substring(0, index);
                s = s.Substring(index + 1);
                return result;
            }
        }
        public static string SplitFirst(this string s, out string rest, char separator = ' ') {
            int index = s.IndexOf(separator);
            if (index == -1) {
                rest = "";
                return s;
            } else {
                rest = s.Substring(index + 1);
                return s.Substring(0, index);
            }
        }
    }
    class CommandString {
        public bool GetArg(int index, out string result)
        {
            if (args.Length > index)
            {
                result = args[index];
                return true;
            }
            result = null;
            return false;
        }
        private string command;
        private string[] args;
        public CommandString(string s) {
            var parts = s.Split(" ");
            command = parts[0];

            Array.Copy(parts, 1, this.args, 0, parts.Length-1);
        }
    }

}
