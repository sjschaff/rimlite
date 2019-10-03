using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace BB
{
    [UnityEditor.InitializeOnLoad]
    public static class BB
    {
        static BB()
        {
            UnityEngine.Application.SetStackTraceLogType(
               UnityEngine.LogType.Log, UnityEngine.StackTraceLogType.None);
        }

        [Conditional("DEBUG")]
        public static void Assert(bool a, string msg = null)
        {
            if (!a)
            {
                UnityEngine.Debug.LogAssertion(msg);
                Log(msg, $"Assert Failed: {msg}");
                // Note if a debugger is not attacked this will prompt
                // to attach a debugger, might be handy
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
        }

        [Conditional("DEBUG")]
        public static void AssertNotNull<T>(T t, string msg = null) where T : class
            => Assert(t != null, msg ?? typeof(T).Name + " != null");

        [Conditional("DEBUG")]
        public static void AssertNull<T>(T t, string msg = null) where T : class
            => Assert(t == null, msg ?? typeof(T).Name + " == null");

        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }

        public static IEnumerable<TEnum> Enums<TEnum>() where TEnum : Enum
        {
            foreach (TEnum t in Enum.GetValues(typeof(TEnum)))
                yield return t;
        }

        private static void Log(string msg, string level)
        {
            // Something (probably unity) is injecting an extra
            // new line so we'll skip it here
            Debug.Write("[" + level + "]: " + msg);
        }

        public static void LogInfo(string msg)
        {
            UnityEngine.Debug.Log(msg);
            Log(msg, "Info");
        }

        public static void LogWarning(string msg)
        {
            UnityEngine.Debug.LogError(msg);
            Log(msg, "Warning");
        }

        public static void LogError(string msg)
        {
            UnityEngine.Debug.LogError(msg);
            Log(msg, "Error");
        }
    }
}