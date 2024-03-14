using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class ArchetypeRule : IGlobalRule {
        public GlobalRuleEvaluationTiming Timing => GlobalRuleEvaluationTiming.PreCardEvaluation;
        public static readonly float PUNISHMENT_PER_OVERAGE = -10f;
        public void Apply(Evaluation evaluation) {
            Save.state.archetypeIdentities = new Dictionary<string, float>();
            foreach (var card in Save.state.cards) {
                foreach (var membership in card.archetypes) {
                    Save.state.AddArchetypeSlotMember(membership);
                }
            }
            var totalArchetypeAlignmentValue = 0f;
            foreach (var archetype in Database.instance.archetypes) {
                var maxMembership = 0f;
                var totalFulfillment = 0f;
                var maxFulfillment = 0f;
                foreach (var slot in archetype.slots) {
                    var membership = Save.state.GetArchetypeSlotMembership(archetype.id, slot.Key);
                    var fulfillment = MathF.Min(1f, membership / slot.Value.count);
                    maxMembership = MathF.Max(maxMembership, fulfillment);
                    totalFulfillment += fulfillment;
                    maxFulfillment += slot.Value.count;
                }
                var averageFulfillment = totalFulfillment / maxFulfillment;
                totalArchetypeAlignmentValue += averageFulfillment * archetype.value;
                Save.state.archetypeIdentities[archetype.id] = maxMembership;
            }
            Evaluation.Active.SetScore(ScoreReason.ArchetypeValue, totalArchetypeAlignmentValue);
            var totalOveragePunishment = 0f;
            foreach (var pickedCard in Save.state.CardsJustChosen()) {
                foreach (var membership in pickedCard.archetypes) {
                    var count = Save.state.GetArchetypeSlotMembership(membership);
                    var slot = Database.instance.archetypeDict[membership.archetypeId].slots[membership.slotId];
                    var overage = count - slot.count;
                    totalOveragePunishment += overage * PUNISHMENT_PER_OVERAGE;
                }
            }
            Evaluation.Active.SetScore(ScoreReason.ArchetypeSlotFull, totalOveragePunishment);
        }
    }
}
