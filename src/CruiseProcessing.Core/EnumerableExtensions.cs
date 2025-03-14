using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace CruiseProcessing
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> DistinctBy<T, tKey>(this IEnumerable<T> @this, Func<T, tKey> keySelector)
        {
            using IEnumerator<T> enumerator = @this.GetEnumerator();


            if (enumerator.MoveNext())
            {
                var set = new HashSet<tKey>(7);
                do
                {
                    T element = enumerator.Current;
                    if (set.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
                while (enumerator.MoveNext());
            }
        }
    }
}
