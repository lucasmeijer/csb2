using System.Collections.Generic;
using System.Text;

namespace csb2
{
    static class Extensions
    {
        public static string ConcatAll(this IEnumerable<object> objects)
        {
            var sb = new StringBuilder();
            foreach (var o in objects)
                sb.Append(o.ToString());
            return sb.ToString();
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> many, params T[] extra)
        {
            foreach (var o in many)
                yield return o;
            foreach(var e in extra)
                yield return e;
        }

        public static string SeperateWith(this IEnumerable<object> many, string inbetween)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var m in many)
            {
                if (!first)
                    sb.Append(inbetween);
                sb.Append(m);
                first = false;
            }
            return sb.ToString();
        }
    }
}