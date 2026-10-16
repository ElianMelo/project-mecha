using System;
using System.Reflection;
using UnityEngine;

namespace Mecha.Tests
{
    // Gameplay currently lives in Unity's predefined Assembly-CSharp. Test asmdefs
    // cannot reference that assembly; keep this bridge here instead of moving production code.
    public static class GameCode
    {
        public static Type Type(string name) => System.Type.GetType(name + ", Assembly-CSharp", true);
        public static object New(string name) => Activator.CreateInstance(Type(name));
        public static ScriptableObject Asset(string name) => ScriptableObject.CreateInstance(Type(name));
        public static T Get<T>(object target, string field) => (T)Field(target, field).GetValue(target);
        public static void Set(object target, string field, object value) => Field(target, field).SetValue(target, value);
        public static object Call(object target, string method, params object[] args)
        {
            var type = target as Type ?? target.GetType();
            var member = type.GetMethod(method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                ?? throw new MissingMethodException(type.FullName, method);
            return member.Invoke(target is Type ? null : target, args);
        }
        public static Array Array(string type, params object[] values)
        {
            var array = System.Array.CreateInstance(Type(type), values.Length);
            for (int i = 0; i < values.Length; i++) array.SetValue(values[i], i);
            return array;
        }
        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name)
            ?? throw new MissingFieldException(target.GetType().FullName, name);
    }
}
