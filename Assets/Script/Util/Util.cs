using System;

namespace BB
{
    public static class BB
    {
        public static void Assert(bool a, string msg = null)
        {
            // TODO: make debug only
            if (!a)
            {
                throw new Exception("Assert failed - " + msg);
            }
        }

        public static void AssertNotNull<T>(T t, string msg = null) where T : class
            => Assert(t != null, msg);

        public static void AssertNull<T>(T t, string msg = null) where T : class
            => Assert(t == null, msg);

        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }
    }
}