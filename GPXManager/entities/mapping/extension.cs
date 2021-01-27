using System;

namespace GPXManager.entities.mapping
{
    internal static class extension
    {
        public static void With<T>(this T obj, Action<T> a)
        {
            a(obj);
        }
    }
}