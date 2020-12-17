using Newtonsoft.Json;
using System;
using System.IO;

namespace Common {
    public static class Common {
        public static bool StartsWith(this string s, string str, out string trimmed) {
            if (s.StartsWith(str)) {
                trimmed = s.Substring(str.Length).TrimStart();
                return true;
            } else {
                trimmed = null;
                return false;
            }
        }
        public static void Save<T>(T t) {
            var f = $"./{typeof(T).Name}.json";
            Console.WriteLine(Path.GetFullPath(f));

            File.WriteAllText(f, JsonConvert.SerializeObject(t, Formatting.Indented, new JsonSerializerSettings() {
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.All
            }));
        }
        public static T Load<T>() {
            var f = $"{typeof(T).Name}.json";
            Console.WriteLine(Path.GetFullPath(f));
            if (File.Exists(f)) {
                T t = JsonConvert.DeserializeObject<T>(File.ReadAllText(f));
                return t;
            }
            return default(T);
        }
        public static bool Load<T>(out T t) {
            var f = $"{typeof(T).Name}.json";
            Console.WriteLine(Path.GetFullPath(f));
            if (File.Exists(f)) {
                t = JsonConvert.DeserializeObject<T>(File.ReadAllText(f));
                return true;
            }
            t = default(T);
            return false;
        }
    }
}
