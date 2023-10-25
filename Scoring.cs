using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public interface IGlobalRule {
        public bool ShouldApply { get; }
        public float Apply(Path path);
    }
    internal class Scoring {
        protected static IGlobalRule[] GlobalRules;
        static Scoring() {
            var globalRuleTypes = typeof(Scoring).Assembly.GetTypes().Where(x => typeof(IGlobalRule).IsAssignableFrom(x) && typeof(IGlobalRule) != x);
            GlobalRules = globalRuleTypes.Select(x => (IGlobalRule)Activator.CreateInstance(x)).ToArray();
        }
        public static float EvaluateGlobalRules(Path path) {
            var globalEvaluation = 0f;
            foreach (var globalRule in GlobalRules) {
                if (globalRule.ShouldApply) {
                    globalEvaluation += globalRule.Apply(path);
                }
            }
            return globalEvaluation;
        }

        public static float ScorePath(Path path) {
            // Things to think about:
            // - How many elites can I do this act? 
            // ✔ What is the largest number of elites available?
            // ✔ Can I dodge all elites?
            // - Will this path kill me?
            // - Do we need to go to a shop?
            // - Do we have tiny chest / serpent head?
            // - Do we need green key?
            // - Does this path have an off-ramp?
            // - Are we looking for any events? (golden idol considerations etc)
            // - Do we have fight metascaling (ritual dagger, genetic algorithm, etc)
            // - What is our expected health loss per fight / elite

            // ROUGH Rules:
            //  - you get ~10 points for surviving acts 1, 2 and 3
            //  - you get 2 points for the first 4 upgrades, then 1 for the next 6
            //  - 1 point per relic up to 15
            //  - .5 points per card reward
            //  - card removes?
            //  - 1 point per hundred gold
            //  - 1 point per key
            //  - 1 point per 10 health
            var points = 0f;

            points += 10f * (1f - path.Risk);

            var upgrades = path.expectedUpgrades[^1];
            if (upgrades <= 4) {
                points += upgrades * 2f;
            }
            else {
                points += 8 + MathF.Min(upgrades - 4, 6);
            }

            points += MathF.Min(path.expectedRewardRelics[^1], 15f);

            points += .5f * path.expectedCardRewards[^1];

            points += Save.state.gold / 100f;

            points += Save.state.has_emerald_key ? 1 : 0;
            points += Save.state.has_ruby_key ? 1 : 0;
            points += Save.state.has_sapphire_key ? 1 : 0;

            var effectiveHealth = Evaluators.GetEffectiveHealth();
            points += effectiveHealth / 10f;

            if (Save.state.act_num == 3 && !Save.state.has_emerald_key && !path.hasMegaElite) {
                points = 0f;
            }

            return points;
        }
    }
}
