using System;
using System.Collections.Generic;
namespace SpongeLake.SpongeLake {
    public class TypeDict<T> {
        public Dictionary<Type, T> components;
        public bool Has<U>() where U : T => components.ContainsKey(typeof(U));
        public bool Has<U>(out U value) where U : T {
            bool result = components.TryGetValue(typeof(U), out T value2);
            value = (U)value2;
            return result;
        }

        public U Get<U>() where U : T {
            return (U)components[typeof(U)];
        }
        public void Set<U>(U value) where U : T {
            components[typeof(U)] = value;
        }
        public IEnumerable<T> Values => components.Values;
    }
}
