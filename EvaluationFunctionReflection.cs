using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace ProjectPumpernickle {
    internal class EvaluationFunctionReflection {
        protected static Dictionary<string, Func<Card, int, float>> cardCache = new Dictionary<string, Func<Card, int, float>>();
        protected static Dictionary<string, Func<Card, int, float>> upgradeCache = new Dictionary<string, Func<Card, int, float>>();
        protected static Dictionary<string, Func<Relic, float>> relicCache = new Dictionary<string, Func<Relic, float>>();
        protected static Dictionary<string, Func<float>> fightCache = new Dictionary<string, Func<float>>();
        public static Func<Card, int, float> GetCardEvalFunctionCached(string cardId) {
            return GetFunctionCached(cardId, cardCache, CardFunctionFactory(typeof(CardFunctions), x => x.bias));
        }
        public static Func<Card, int, float> GetUpgradeHealthPerFightFunctionCached(string cardId) {
            return GetFunctionCached(cardId, upgradeCache, CardFunctionFactory(typeof(CardUpgradeFunctions), x => x.upgradeHealthPerFight));
        }
        public static Func<Relic, float> GetRelicEvalFunctionCached(string relicId) {
            return GetFunctionCached(relicId, relicCache, GetRelicEvalFunction);
        }
        public static Func<Relic, float> GetFightEvalFunctionCached(string encounterId) {
            return GetFunctionCached(encounterId, relicCache, GetFightEvalFunction);
        }
        private static T GetFunctionCached<T>(string id, Dictionary<string, T> cache, Func<string, T> FunctionFactory) {
            if (cache.TryGetValue(id, out var func)) {
                return func;
            }
            func = FunctionFactory(id);
            cache.Add(id, func);
            return func;
        }
        public static string SanitizeId(string id) {
            return id.Replace(" ", "").Replace("-", "").Replace(".", "").Replace("'", "");
        }
        protected static Func<string, Func<Card, int, float>> CardFunctionFactory(Type functionSource, Func<Card, float> bias) {
            var sourceCapture = functionSource;
            var biasCapture = bias;
            return (string cardId) => {
                cardId = SanitizeId(cardId);
                var method = functionSource.GetMethod(cardId, BindingFlags.Static | BindingFlags.Public);
                if (method == null) {
                    return (Card c, int i) => {
                        return 0;
                    };
                }
                return (Card c, int i) => {
                    return (float)method.Invoke(null, new object[] { c, i }) + bias(c);
                };
            };
        }
        protected static Func<Relic, float> GetRelicEvalFunction(string relicId) {
            relicId = SanitizeId(relicId);
            var method = typeof(RelicFunctions).GetMethod(relicId, BindingFlags.Static | BindingFlags.Public);
            if (method == null) {
                return (Relic r) => {
                    return 0;
                };
            }
            return (Relic r) => {
                return (float)method.Invoke(null, new object[] { r }) + r.bias;
            };
        }
        protected static Func<float> GetFightEvalFunction(string encounterId) {
            var method = typeof(FightSimulators).GetMethod(encounterId, BindingFlags.Static | BindingFlags.Public);
            if (method == null) {
                return () => {
                    return 0;
                };
            }
            return () => {
                return (float)method.Invoke(null, null);
            };
        }
    }
}
