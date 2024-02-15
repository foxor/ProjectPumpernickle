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
        protected static Dictionary<string, Func<Card, int, float, float>> upgradeCache = new Dictionary<string, Func<Card, int, float, float>>();
        protected static Dictionary<string, Func<Relic, float>> relicEvalCache = new Dictionary<string, Func<Relic, float>>();
        protected static Dictionary<string, Func<IEnumerable<RewardOptionPart>>> relicSplitCache = new Dictionary<string, Func<IEnumerable<RewardOptionPart>>>();
        protected static Dictionary<string, Action<string, RewardContext>> relicPickCache = new Dictionary<string, Action<string, RewardContext>>();
        protected static Dictionary<string, Func<int, float>> eventValueCache = new Dictionary<string, Func<int, float>>();
        protected static Dictionary<string, Func<int, float>> encounterSimulationCache = new Dictionary<string, Func<int, float>>();
        public static Func<Card, int, float> GetCardEvalFunctionCached(string cardId) {
            return GetFunctionCached(cardId, cardCache, CardFunctionFactory(typeof(CardFunctions)));
        }
        public static Func<Card, int, float, float> GetUpgradeFunctionCached(string cardId) {
            return GetFunctionCached(cardId, upgradeCache, CardUpgradeFunctionFactory(typeof(CardUpgradeFunctions)));
        }
        public static Func<Relic, float> GetRelicEvalFunctionCached(string relicId) {
            return GetFunctionCached(relicId, relicEvalCache, GetRelicEvalFunction);
        }
        public static Func<IEnumerable<RewardOptionPart>> GetRelicOptionSplitFunctionCached(string relicId) {
            return GetFunctionCached(relicId, relicSplitCache, GetRelicSplitFunction);
        }
        public static Action<string, RewardContext> GetRelicOnPickedFunctionCached(string relicId) {
            return GetFunctionCached(relicId, relicPickCache, GetRelicPickFunction);
        }
        public static Func<int, float> GetEventValueFunctionCached(string eventName) {
            return GetFunctionCached(eventName, eventValueCache, GetEventValueFunction);
        }
        public static Func<int, float> GetEncounterSimulationFunctionCached(string encounterId) {
            return GetFunctionCached(encounterId, encounterSimulationCache, GetEncounterSimulationFunction);
        }
        private static T GetFunctionCached<T>(string id, Dictionary<string, T> cache, Func<string, T> FunctionFactory) {
            if (cache.TryGetValue(id, out var func)) {
                return func;
            }
            func = FunctionFactory(id);
            lock(cache) {
                if (!cache.ContainsKey(id)) {
                    cache.Add(id, func);
                }
            }
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
        protected static Func<string, Func<Card, int, float>> CardFunctionFactory(Type functionSource) {
            var sourceCapture = functionSource;
            return (string cardId) => {
                cardId = SanitizeId(cardId);
                var method = sourceCapture.GetMethod(cardId, BindingFlags.Static | BindingFlags.Public);
                return (Card c, int i) => {
                    var value = (float)method.Invoke(null, new object[] { c, i });
                    value = Evaluators.ExtraCardValue(c, value, i);
                    return value;
                };
            };
        }
        protected static Func<string, Func<Card, int, float, float>> CardUpgradeFunctionFactory(Type functionSource) {
            var sourceCapture = functionSource;
            return (string cardId) => {
                cardId = SanitizeId(cardId);
                var method = sourceCapture.GetMethod(cardId, BindingFlags.Static | BindingFlags.Public);
                return (Card c, int i, float f) => {
                    var value = Evaluators.ExtraCardUpgradeValue(c, f);
                    value = (float)method.Invoke(null, new object[] { c, i, value });
                    return value;
                };
            };
        }
        protected static Func<Relic, float> GetRelicEvalFunction(string relicId) {
            relicId = SanitizeId(relicId);
            var method = typeof(RelicFunctions).GetMethod(relicId, BindingFlags.Static | BindingFlags.Public);
            return (Relic r) => {
                return (float)method.Invoke(null, new object[] { r });
            };
        }
        protected static Func<IEnumerable<RewardOptionPart>> GetRelicSplitFunction(string relicId) {
            var messageId = relicId;
            var sanitizedId = SanitizeId(relicId);
            if (!RelicOptionSplitFunctions.MultiOptionRelics.Contains(sanitizedId)) {
                return () => {
                    return new RewardOptionPart[] {
                        new RewardOptionPart() {
                            value = messageId,
                        }
                    };
                };
            }
            var method = typeof(RelicOptionSplitFunctions).GetMethod(sanitizedId, BindingFlags.Static | BindingFlags.Public);
            return () => {
                return (IEnumerable<RewardOptionPart>)method.Invoke(null, null);
            };
        }
        protected static Action<string, RewardContext> GetRelicPickFunction(string relicId) {
            var messageId = relicId;
            var sanitizedId = SanitizeId(relicId);
            if (!RelicOptionSplitFunctions.MultiOptionRelics.Contains(sanitizedId)) {
                return (string paramters, RewardContext context) => {};
            }
            var method = typeof(RelicPickFunctions).GetMethod(sanitizedId, BindingFlags.Static | BindingFlags.Public);
            return (string parameters, RewardContext r) => {
                method.Invoke(null, new object[] { parameters, r });
            };
        }
        protected static Func<int, float> GetEventValueFunction(string eventName) {
            eventName = SanitizeId(eventName);
            var method = typeof(EventValueFunctions).GetMethod(eventName, BindingFlags.Static | BindingFlags.Public);
            var @event = Database.instance.eventDict[eventName];
            return (int i) => {
                return ((float)method.Invoke(null, new object[] { @event, i })) + @event.bias;
            };
        }
        protected static Func<int, float> GetEncounterSimulationFunction(string encounterId) {
            encounterId = "ENC" + SanitizeId(encounterId);
            var method = typeof(EncounterSimulationFunctions).GetMethod(encounterId, BindingFlags.Static | BindingFlags.Public);
            return (int floorNum) => {
                return ((float)method.Invoke(null, new object[] { floorNum }));
            };
        }
    }
}
