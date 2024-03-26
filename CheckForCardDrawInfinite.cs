using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class CheckForCardDrawInfinite : IGlobalRule {
        public GlobalRuleEvaluationTiming Timing => GlobalRuleEvaluationTiming.VeryEarly;
        public class InfiniteSubpackage {
            public int cost;
            public int deltaHandSize;
            public int cardsToDiscard;
            public float damage;
            public float block;
            public static InfiniteSubpackage FromCardDrawCard(Card card) {
                var discard = (int)card.setup.GetValueOrDefault(ScoreReason.Discard.ToString());
                return new InfiniteSubpackage() {
                    cost = card.intCost,
                    deltaHandSize = (int)card.tags[Tags.CardDraw.ToString()] - discard - 1,
                    damage = card.tags.GetValueOrDefault(Tags.Damage.ToString()),
                    block = card.tags.GetValueOrDefault(Tags.Block.ToString()),
                    cardsToDiscard = 1 + discard,
                };
            }
            public static readonly float INFINITE_EFFICIENCY = 1000f;
            public static readonly float COST_PENALTY = 0.001f;
            public float DrawEfficiency() {
                if (cost == 0) {
                    return INFINITE_EFFICIENCY * deltaHandSize + (COST_PENALTY * cardsToDiscard);
                }
                return (deltaHandSize * 1f / cost) - (COST_PENALTY * cost) + (COST_PENALTY * cardsToDiscard);
            }
            public float EnergyEfficiency() {
                if (cardsToDiscard == 0) {
                    return INFINITE_EFFICIENCY * -cost;
                }
                return (-cost * 1f / cardsToDiscard) - (COST_PENALTY * cardsToDiscard);
            }
        }
        public IEnumerable<InfiniteSubpackage> CardDrawPackages() {
            // TODO: zero cost cards
            return Save.state.cards
                .Where(x => x.tags.ContainsKey(Tags.CardDraw.ToString()))
                .Select(InfiniteSubpackage.FromCardDrawCard);
        }
        public IEnumerable<InfiniteSubpackage> EnergyPackages() {
            if (Save.state.cards.Any(x => x.id.Equals("Concentrate"))) {
                yield return new InfiniteSubpackage() {
                    cardsToDiscard = 3,
                    cost = -2,
                    deltaHandSize = -3,
                };
            }
        }
        public int NetEnergy(IEnumerable<InfiniteSubpackage> infinite) {
            return infinite.Select(x => x.cost).Sum();
        }
        public int NetCards(IEnumerable<InfiniteSubpackage> infinite) {
            return infinite.Select(x => x.deltaHandSize).Sum();
        }
        public bool IsInfinite(IEnumerable<InfiniteSubpackage> infinite) {
            var qualified = true;
            qualified &= NetEnergy(infinite) <= 0;
            qualified &= NetCards(infinite) >= 0;
            qualified &= infinite.Where(x => x.cardsToDiscard > 0).Count() > 1;
            return qualified;
        }
        public void Apply(Evaluation evaluation) {
            // Sort the card draw cards by efficiency
            // Qualify energy positive packages
            // Sort the card positive packages by how many cards they take
            // in a loop
            //  - consider adding the next card draw card
            //  - if your energy is negative, add the next energy package
            //  - if you are infinite, save this as the best infinite
            //  - if you run out of energy sources or card draw cards, exit loop
            // if you saved an infinite, set your max deck size based on how many cards
            var cardDraw = CardDrawPackages().OrderByDescending(x => x.DrawEfficiency()).ToList();
            var energy = EnergyPackages().OrderByDescending(x => x.EnergyEfficiency()).ToList();
            var bestInfinite = new List<InfiniteSubpackage>();
            var cardDrawIndex = 0;
            var energyIndex = 0;
            var bestDamagePerCard = 0f;
            var bestBlockPerCard = 0f;
            var hasExcessDraw = false;
            while (true) {
                var failed = false;
                if (cardDrawIndex >=  cardDraw.Count) {
                    break;
                }
                var consideringDraw = cardDraw[cardDrawIndex++];
                var considering = bestInfinite.Append(consideringDraw);
                var consideringEnergy = new List<InfiniteSubpackage>();
                var resetEnergyIndex = energyIndex;
                var netEnergy = NetEnergy(considering);
                while (netEnergy > 0) {
                    if (energyIndex >= energy.Count) {
                        failed = true;
                        break;
                    }
                    consideringEnergy.Add(energy[energyIndex++]);
                    considering = bestInfinite.Concat(consideringEnergy).Append(consideringDraw);
                    netEnergy = NetEnergy(considering);
                }
                if (failed) {
                    energyIndex = resetEnergyIndex;
                    continue;
                }
                var netCards = NetCards(considering);
                if (netCards < 0 && netEnergy > 0) {
                    energyIndex = resetEnergyIndex;
                    continue;
                }
                bestInfinite.Add(consideringDraw);
                bestInfinite.AddRange(consideringEnergy);
                if (IsInfinite(bestInfinite)) {
                    var totalCards = bestInfinite.Select(x => x.cardsToDiscard).Sum();
                    if (netCards > 0) {
                        hasExcessDraw = true;
                        var damage = Evaluators.HighestZeroCostDamage();
                        bestDamagePerCard = MathF.Max(bestDamagePerCard, damage / (totalCards + 1));
                    }
                    if (netCards > 0 && netEnergy > 0) {
                        bestBlockPerCard = MathF.Max(bestBlockPerCard, 0.5f);
                        bestDamagePerCard = MathF.Max(bestDamagePerCard, 0.5f);
                    }
                    bestDamagePerCard = MathF.Max(bestDamagePerCard, bestInfinite.Select(x => x.damage).Average());
                    bestBlockPerCard = MathF.Max(bestBlockPerCard, bestInfinite.Select(x => x.block).Average());
                    Save.state.buildingInfinite = true;
                    Save.state.infiniteMaxSize = 9 + totalCards;
                    Save.state.infiniteBlockPerCard = bestBlockPerCard;
                    Save.state.infiniteDoesDamage = bestDamagePerCard > 0;
                    var cardsToClear = Save.state.cards.Count() - Save.state.infiniteMaxSize;
                    // FIXME: cost to remove these?
                    cardsToClear -= (int)Evaluators.PermanentDeckSizeOffset();
                    var nonpermanentsToClear = Evaluators.NonpermanentCards().OrderBy(x => x.intCost).Take(cardsToClear);
                    var clearCost = nonpermanentsToClear.Where(x => x.intCost != int.MaxValue).Select(x => x.intCost).Sum();
                    var clearTurn = (int)MathF.Max(1, MathF.Ceiling((clearCost - Evaluators.ExtraPerFightEnergy()) / Evaluators.PerTurnEnergy()));
                    Save.state.earliestInfinite = clearTurn;
                }
            }
        }
    }
}
