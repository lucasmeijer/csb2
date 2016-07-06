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

    }
}