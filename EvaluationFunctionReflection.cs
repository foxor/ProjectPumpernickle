using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace ProjectPumpernickle {
    internal class EvaluationFunctionReflection {
        protected static Dictionary<string, Func<Card, int, float>> cardCache = new Dictionary<string, Func<Card, int, float>>();
        protected static Dictionary<string, Func<Card, int, float>> upgradeCache = new Dictionary<string, Func<Card, int, float>>();
        protected static Dictionary<string, Func<Relic, float>> relicEvalCache = new Dictionary<string, Func<Relic, float>>();
        protected static Dictionary<string, Action<RewardContext>> relicPickCache = new Dictionary<string, Action<RewardContext>>();
        protected static Dictionary<string, Func<int, float>> eventValueCache = new Dictionary<string, Func<int, float>>();
        public static Func<Card, int, float> GetCardEvalFunctionCached(string cardId) {
            return GetFunctionCached(cardId, cardCache, CardFunctionFactory(typeof(CardFunctions), x => x.bias));
        }
        public static Func<Card, int, float> GetUpgradePowerMultiplierFunctionCached(string cardId) {
            return GetFunctionCached(cardId, upgradeCache, CardFunctionFactory(typeof(CardUpgradeFunctions), x => x.upgradePowerMultiplier));
        }
        public static Func<Relic, float> GetRelicEvalFunctionCached(string relicId) {
            return GetFunctionCached(relicId, relicEvalCache, GetRelicEvalFunction);
        }
        public static Action<RewardContext> GetRelicOnPickedFunctionCached(string relicId) {
            return GetFunctionCached(relicId, relicPickCache, GetRelicPickFunction);
        }
        public static Func<int, float> GetEventValueFunctionCached(string eventName) {
            return GetFunctionCached(eventName, eventValueCache, GetEventValueFunction);
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
            return id
                .Replace(" ", "")
                .Replace("-", "")
                .Replace(".", "")
                .Replace("'", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("!", "")
                .Replace("?", "");
        }
        protected static Func<string, Func<Card, int, float>> CardFunctionFactory(Type functionSource, Func<Card, float> bias) {
            var sourceCapture = functionSource;
            var biasCapture = bias;
            return (string cardId) => {
                cardId = SanitizeId(cardId);
                var method = sourceCapture.GetMethod(cardId, BindingFlags.Static | BindingFlags.Public);
                if (method == null) {
                    return (Card c, int i) => {
                        return biasCapture(c);
                    };
                }
                return (Card c, int i) => {
                    var value = (float)method.Invoke(null, new object[] { c, i });
                    value += biasCapture(c);
                    if (c.bottled && c.tags.TryGetValue(Tags.BottleEquity.ToString(), out var bottleValue)) {
                        value += bottleValue;
                    }
                    return value;
                };
            };
        }
        protected static Func<Relic, float> GetRelicEvalFunction(string relicId) {
            relicId = SanitizeId(relicId);
            var method = typeof(RelicFunctions).GetMethod(relicId, BindingFlags.Static | BindingFlags.Public);
            if (method == null) {
                return (Relic r) => {
                    return r.bias;
                };
            }
            return (Relic r) => {
                return (float)method.Invoke(null, new object[] { r }) + r.bias;
            };
        }
        protected static Action<RewardContext> GetRelicPickFunction(string relicId) {
            relicId = SanitizeId(relicId);
            var method = typeof(RelicPickFunctions).GetMethod(relicId, BindingFlags.Static | BindingFlags.Public);
            if (method == null) {
                return (RewardContext r) => {
                };
            }
            return (RewardContext r) => {
                method.Invoke(null, new object[] { r });
            };
        }
        protected static Func<int, float> GetEventValueFunction(string eventName) {
            eventName = SanitizeId(eventName);
            var method = typeof(EventValueFunctions).GetMethod(eventName, BindingFlags.Static | BindingFlags.Public);
            var @event = Database.instance.eventDict[eventName];
            if (method == null) {
                return (int i) => @event.bias;
            }
            return (int i) => {
                return ((float)method.Invoke(null, new object[] { @event, i })) + @event.bias;
            };
        }
    }
}
