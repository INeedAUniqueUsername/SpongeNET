using System;
using System.Collections.Generic;
using System.Text;

namespace SpongeNET
{
    static class Helper
    {
        public static string Try(this object data, string member, string fallback = "")
        {
            try
            {
                return ((dynamic)data)[member];
            } catch
            {
                return fallback;
            }
        }
        public static int Try(this object data, string member, int fallback)
        {
            try
            {
                return ((dynamic)data)[member];
            }
            catch
            {
                return fallback;
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
