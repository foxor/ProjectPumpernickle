using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace ProjectPumpernickle {
    internal class CardFunctionReflection {
        protected static Dictionary<String, Func<Card, int, float>> cache = new Dictionary<string, Func<Card, int, float>>();
        public static Func<Card, int, float> GetEvalFunctionCached(string cardId) {
            if (cache.TryGetValue(cardId, out var func)) {
                return func;
            }
            func = GetEvalFunction(cardId);
            cache.Add(cardId, func);
            return func;
        }
        protected static Func<Card, int, float> GetEvalFunction(string cardId) {
            cardId = cardId.Replace(" ", "").Replace("-", "").Replace(".", "");
            var method = typeof(CardFunctions).GetMethod(cardId, BindingFlags.Static | BindingFlags.Public);
            if (method == null) {
                return (Card c, int i) => {
                    return 0;
                };
            }
            return (Card c, int i) => {
                return (float)method.Invoke(null, new object[] { c, i }) + c.bias;
            };
        }
    }
}
