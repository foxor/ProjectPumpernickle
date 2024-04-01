using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class UpgradeBestInSlot : IGlobalRule {
        public GlobalRuleEvaluationTiming Timing => GlobalRuleEvaluationTiming.PostCardEvaluation;
        public static readonly float POINTS_FOR_UPGRADING_BEST_CARD = 2f;
        public static readonly float BEST_CARD_POINTS_MULTIPLIER = 0.05f;

        public static readonly Dictionary<string, float> INITIALLY_RELEVANT_TAGS = new Dictionary<string, float>() {
            { "Damage", 1f },
            { "Block", 1f },
            { "Weak", .3f },
        };
        public void Apply(Evaluation evaluation) {
            var tagRelevance = INITIALLY_RELEVANT_TAGS.ToDictionary(x => x.Key, x => x.Value);
            foreach (var archetypeIdentity in Save.state.archetypeIdentities) {
                var archetype = Database.instance.archetypeDict[archetypeIdentity.Key];
                foreach (var tag in archetype.slots.Keys) {
                    if (!tagRelevance.TryGetValue(tag, out var relevance)) {
                        tagRelevance[tag] = archetypeIdentity.Value;
                    }
                    else {
                        tagRelevance[tag] = MathF.Max(relevance, archetypeIdentity.Value);
                    }
                }
            }
            var bestInSlot = tagRelevance.Keys.ToDictionary(x => x, x =>
                Save.state.cards
                    .Where(c => c.tags.ContainsKey(x))
                    .OrderByDescending(x => x.evaluatedScore)
                    .FirstOrDefault()
            );
            var totalUpgradeReward = 0f;
            var totalMultReward = 0f;
            foreach (var card in Save.state.cards) {
                foreach (var bestCard in bestInSlot) {
                    if (bestCard.Value == card) {
                        if (card.upgrades > 0) {
                            totalUpgradeReward += tagRelevance[bestCard.Key] * POINTS_FOR_UPGRADING_BEST_CARD;
                        }
                        totalMultReward += tagRelevance[bestCard.Key] * BEST_CARD_POINTS_MULTIPLIER * card.evaluatedScore;
                    }
                }
            }
            Evaluation.Active.SetScore(ScoreReason.UpgradeBestInSlot, totalUpgradeReward);
            Evaluation.Active.SetScore(ScoreReason.BestInSlotMultiplier, totalMultReward);
        }
    }
}
