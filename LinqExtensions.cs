using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal static class LinqExtensions {
        public static IEnumerable<T> Merge<T>(this IEnumerable<IEnumerable<T>> source) {
            foreach (var enumerable in source) {
                foreach (var item in enumerable) {
                    yield return item;
                }
            }
        }
        public static float[] Sum(this IEnumerable<float[]> source) {
            var r = new float[source.First().Length];
            foreach (var item in source) {
                for (int i = 0; i < r.Length; i++) {
                    r[i] += item[i];
                }
            }
            return r;
        }
    }
}
