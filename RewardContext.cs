using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public class RewardOption {
        public RewardType rewardType;
        public string[] values;
        public string[] eventCost;
        public int cost;
        public bool skippable = true;
        public int amount;
        public int[] hpCost;
        public string[] advice;
    }
    public class RewardContext : IDisposable {
        public List<string> relics = new List<string>();
        public List<int> addedCardIndicies = new List<int>();
        public int goldAdded;
        public List<int> potionIndicies = new List<int>();
        public List<string> description = new List<string>();
        public bool tookGreenKey;
        public bool tookBlueKey;
        public List<Card> cardsRemoved = new List<Card>();
        public List<int> removedCardIndicies = new List<int>();
        public int maxHealthLost;
        public int healthLost;
        public int bonusCardRewards;
        public List<int> upgradeIndicies = new List<int>();
        public string relicRemoved = null;
        public int relicRemoveIndex = -1;
        public bool isInvalid;
        public Card bottled;
        public bool gainedMembershipCard;
        public IRewardStatisticsGroup statisticsGroup;
        public RewardContext(in List<RewardOption> rewardOptions, List<int> rewardIndicies, bool eligibleForBlueKey, bool isShop) {
            for (int i = 0; i < rewardIndicies.Count; i++) {
                var rewardGroup = rewardOptions[i];
                var index = rewardIndicies[i];
                if (index >= rewardGroup.values.Length) {
                    // We're skipping this reward
                    if (eligibleForBlueKey && rewardGroup.rewardType == RewardType.Relic) {
                        description.Add("Take the blue key");
                        Save.state.has_sapphire_key = true;
                        tookBlueKey = true;
                    }
                    else if (!isShop) {
                        if (rewardGroup.rewardType == RewardType.Cards && Save.state.relics.Contains("Singing Bowl")) {
                            description.Add("Take the +2 max hp");
                        }
                        else {
                            description.Add("Skip the " + rewardGroup.rewardType);
                        }
                    }
                    if (!rewardGroup.skippable) {
                        isInvalid = true;
                    }
                    continue;
                }
                var chosen = rewardGroup.values[index];
                var chosenId = chosen.Replace("+", "");
                var cost = rewardGroup.cost;
                if (gainedMembershipCard) {
                    cost -= (rewardGroup.cost / 2);
                }
                Save.state.gold -= cost;
                goldAdded -= cost;
                Save.state.current_health -= rewardGroup.hpCost == null ? 0 : rewardGroup.hpCost[index];
                healthLost += rewardGroup.hpCost == null ? 0 : rewardGroup.hpCost[index];
                bool alreadyAdvised = false;
                if (rewardGroup.advice != null && !string.IsNullOrEmpty(rewardGroup.advice[index])) {
                    description.Add(rewardGroup.advice[index]);
                    alreadyAdvised = true;
                }
                switch (rewardGroup.rewardType) {
                    case RewardType.Cards: {
                        var cardData = Database.instance.cardsDict[chosenId];
                        addedCardIndicies.Add(Save.state.AddCardById(chosenId));
                        if (cardData.tags.TryGetValue(Tags.Damage.ToString(), out var damage)) {
                            Save.state.addedDamagePerTurn = damage / Save.state.cards.Count() * Evaluators.AverageCardsPerTurn();
                        }
                        if (cardData.tags.TryGetValue(Tags.Block.ToString(), out var block)) {
                            Save.state.addedBlockPerTurn = block / Save.state.cards.Count() * Evaluators.AverageCardsPerTurn();
                        }
                        if (cardData.cardType == CardType.Skill) {
                            Save.state.addedSkill = true;
                        }
                        description.Add("Take the " + cardData.name);
                        break;
                    }
                    case RewardType.Gold: {
                        var value = int.Parse(chosen);
                        goldAdded += value;
                        Save.state.gold += value;
                        description.Add("Take the gold");
                        break;
                    }
                    case RewardType.Relic: {
                        relics.Add(chosen);
                        var pickAdviceFn = EvaluationFunctionReflection.GetRelicOnPickedFunctionCached(chosen);
                        Save.state.relics.Add(chosen);
                        description.Add("Take the " + Database.instance.relicsDict[chosen].name);
                        pickAdviceFn(this);
                        if (chosen.Equals("Membership Card")) {
                            gainedMembershipCard = true;
                        }
                        break;
                    }
                    case RewardType.Potion: {
                        description.Add("Take the " + chosen);
                        potionIndicies.Add(Save.state.TakePotion(chosen));
                        break;
                    }
                    case RewardType.Key: {
                        description.Add("Take the " + chosen + " key");
                        switch (chosen) {
                            case "Green": {
                                Save.state.has_emerald_key = true;
                                tookGreenKey = true;
                                break;
                            }
                            default: {
                                throw new System.NotImplementedException();
                            }
                        }
                        break;
                    }
                    case RewardType.CardRemove: {
                        var removeIndex = int.Parse(chosen);
                        cardsRemoved.Add(Save.state.cards[removeIndex]);
                        removedCardIndicies.Add(removeIndex);
                        description.Add("Remove the " + Save.state.cards[removeIndex].name);
                        Save.state.cards.RemoveAt(removeIndex);
                        break;
                    }
                    case RewardType.Event: {
                        var eventCost = rewardGroup.eventCost == null ? "NONE" : rewardGroup.eventCost[index];
                        var rewardHeader = chosen;
                        var colonIndex = chosen.IndexOf(":");
                        if (colonIndex != -1) {
                            rewardHeader = rewardHeader.Substring(0, colonIndex);
                        }
                        HandleEvent(Enum.GetValues<EventRewardElement>().Where(x => x.ToString().Equals(rewardHeader)).Single(), chosen, eventCost, alreadyAdvised);
                        break;
                    }
                    default: {
                        throw new System.NotImplementedException();
                    }
                }
            }
        }

        protected void HandleEvent(EventRewardElement reward, string rewardValue, string cost, bool alreadyAdvised) {
            var line1 = new StringBuilder("Take ");
            var line2 = new StringBuilder();
            switch (reward) {
                case EventRewardElement.RANDOM_COLORLESS_2: {
                    line1.Append("the rare colorless");
                    Evaluators.BestAndWorstRewardResults(Color.Colorless, out var bestCard, out var bestValue, out float worstCaseValue, out float averageCaseValue, Rarity.Rare);
                    addedCardIndicies.Add(Save.state.AddCardById(bestCard.id));
                    //chanceOfOutcome = Evaluators.ChanceOfSpecificCardInReward(Color.Colorless, Rarity.Rare, 1f);
                    //worstCaseValueProportion = worstCaseValue / bestValue;
                    //averageCaseValueProportion = averageCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.THREE_CARDS: {
                    line1.Append("the card reward");
                    bonusCardRewards += 1;
                    break;
                }
                case EventRewardElement.ONE_RANDOM_RARE_CARD: {
                    line1.Append("the random rare card");
                    Evaluators.BestAndWorstRewardResults(Save.state.character.ToColor(), out var bestCard, out var bestValue, out float worstCaseValue, out float averageCaseValue, Rarity.Rare);
                    addedCardIndicies.Add(Save.state.AddCardById(bestCard.id));
                    //chanceOfOutcome = Evaluators.ChanceOfSpecificCard(Save.state.character.ToColor(), Rarity.Rare);
                    //worstCaseValueProportion = worstCaseValue / bestValue;
                    //averageCaseValueProportion = averageCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.REMOVE_CARD: {
                    line1.Append("remove");
                    var removeIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[removeIndex]);
                    removedCardIndicies.Add(removeIndex);
                    line2.Append("Remove the " + Save.state.cards[removeIndex].name);
                    Save.state.cards.RemoveAt(removeIndex);
                    break;
                }
                case EventRewardElement.UPGRADE_CARD: {
                    line1.Append("upgrade");
                    var bestUpgrade = Evaluators.ChooseBestUpgrade(out var _);
                    var upgradeIndex = Save.state.cards.FirstIndexOf(x => x.id.Equals(bestUpgrade));
                    upgradeIndicies.Add(upgradeIndex);
                    Save.state.cards[upgradeIndex].upgrades++;
                    line2.Append("Upgrade the " + Save.state.cards[upgradeIndex].name);
                    break;
                }
                case EventRewardElement.RANDOM_COLORLESS: {
                    line1.Append("the colorless card reward");
                    Evaluators.BestAndWorstRewardResults(Color.Colorless, out var bestCard, out var bestValue, out float worstCaseValue, out float averageCaseValue, Rarity.Uncommon);
                    addedCardIndicies.Add(Save.state.AddCardById(bestCard.id));
                    //chanceOfOutcome = Evaluators.ChanceOfSpecificCard(Color.Colorless, Rarity.Uncommon);
                    //worstCaseValueProportion = worstCaseValue / bestValue;
                    //averageCaseValueProportion = averageCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.TRANSFORM_CARD: {
                    line1.Append("the transform");
                    var removeIndex = int.Parse(rewardValue.Substring(rewardValue.IndexOf(" ") + 1));
                    var card = Save.state.cards[removeIndex];
                    cardsRemoved.Add(card);
                    removedCardIndicies.Add(removeIndex);
                    line2.Append("Transform the " + card.name);
                    Save.state.cards.RemoveAt(removeIndex);

                    var averageCardId = Evaluators.AverageTransformValue(card.cardColor);
                    addedCardIndicies.Add(Save.state.AddCardById(averageCardId));
                    statisticsGroup = new TransformStatisticsGroup() {
                        transformColor = card.cardColor,
                        transformId = averageCardId,
                    };
                    break;
                }
                case EventRewardElement.THREE_SMALL_POTIONS: {
                    line1.Append("the potions");
                    // TODO: this
                    break;
                }
                case EventRewardElement.RANDOM_COMMON_RELIC: {
                    line1.Append("the random common relic");
                    Evaluators.RandomRelicValue(Save.state.character, out var bestRelic, out var bestValue, out float worstCaseValue, Rarity.Common);
                    relics.Add(bestRelic.id);
                    Save.state.relics.Add(bestRelic.id);
                    //chanceOfOutcome = Evaluators.ChanceOfSpecificRelic(Save.state.character, Rarity.Common);
                    //worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.TEN_PERCENT_HP_BONUS: {
                    var bonus = (int)(Save.state.max_health * .1f);
                    line1.Append("the " + bonus + " max hp");
                    maxHealthLost -= bonus;
                    healthLost -= bonus;
                    Save.state.max_health += bonus;
                    Save.state.current_health += bonus;
                    break;
                }
                case EventRewardElement.HUNDRED_GOLD: {
                    line1.Append("the hundred gold");
                    goldAdded = 100;
                    Save.state.gold += 100;
                    break;
                }
                case EventRewardElement.THREE_ENEMY_KILL: {
                    line1.Append("the lament");
                    relics.Add("NeowsBlessing");
                    Save.state.relics.Add("NeowsBlessing");
                    break;
                }
                case EventRewardElement.REMOVE_TWO: {
                    line1.Append("removes");
                    description.Add(line1.ToString());
                    line2.Append("Remove the ");
                    var firstRemoveIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[firstRemoveIndex]);
                    line2.Append(Save.state.cards[firstRemoveIndex].name + " and the ");
                    removedCardIndicies.Add(firstRemoveIndex);
                    Save.state.cards.RemoveAt(firstRemoveIndex);
                    var secondRemoveIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[secondRemoveIndex]);
                    removedCardIndicies.Add(secondRemoveIndex);
                    Save.state.cards.RemoveAt(secondRemoveIndex);
                    line2.Append(Save.state.cards[secondRemoveIndex].name);
                    break;
                }
                case EventRewardElement.TRANSFORM_TWO_CARDS: {
                    line1.Append("transforms");
                    description.Add(line1.ToString());
                    //Evaluators.AverageTransformValue(out var averageCard);

                    line2.Append("Transform the ");
                    var firstRemoveIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[firstRemoveIndex]);
                    line2.Append(Save.state.cards[firstRemoveIndex].name + " and the ");
                    removedCardIndicies.Add(firstRemoveIndex);
                    Save.state.cards.RemoveAt(firstRemoveIndex);
                    var secondRemoveIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[secondRemoveIndex]);
                    removedCardIndicies.Add(secondRemoveIndex);
                    Save.state.cards.RemoveAt(secondRemoveIndex);
                    line2.Append(Save.state.cards[secondRemoveIndex].name);

                    //addedCardIndicies.Add(Save.state.AddCardById(averageCard));
                    //addedCardIndicies.Add(Save.state.AddCardById(averageCard));
                    break;
                }
                case EventRewardElement.ONE_RARE_RELIC: {
                    line1.Append("the rare relic");
                    Evaluators.RandomRelicValue(Save.state.character, out var bestRelic, out var bestValue, out float worstCaseValue, Rarity.Rare);
                    relics.Add(bestRelic.id);
                    Save.state.relics.Add(bestRelic.id);
                    //chanceOfOutcome = Evaluators.ChanceOfSpecificRelic(Save.state.character, Rarity.Rare);
                    //worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.THREE_RARE_CARDS: {
                    line1.Append("the rare card reward");
                    Evaluators.BestAndWorstRewardResults(Save.state.character.ToColor(), out var bestCard, out var bestValue, out float worstCaseValue, out float averageCaseValue, Rarity.Rare);
                    addedCardIndicies.Add(Save.state.AddCardById(bestCard.id));
                    //chanceOfOutcome = Evaluators.ChanceOfSpecificCardInReward(Save.state.character.ToColor(), Rarity.Rare, 1f);
                    //worstCaseValueProportion = worstCaseValue / bestValue;
                    //averageCaseValueProportion = averageCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.TWO_FIFTY_GOLD: {
                    line1.Append("the 250 gold");
                    goldAdded = 250;
                    Save.state.gold += 250;
                    break;
                }
                case EventRewardElement.TWENTY_PERCENT_HP_BONUS: {
                    var bonus = ((int)(Save.state.max_health * .1f)) * 2;
                    line1.Append("the " + bonus + " max hp");
                    maxHealthLost -= bonus;
                    healthLost -= bonus;
                    Save.state.max_health += bonus;
                    Save.state.current_health += bonus;
                    break;
                }
                case EventRewardElement.BOSS_RELIC: {
                    line1.Append("the boss relic swap");
                    Evaluators.RandomRelicValue(Save.state.character, out var bestRelic, out var bestValue, out float worstCaseValue, Rarity.Boss);
                    relics.Add(bestRelic.id);
                    if (Save.state.relics?.Any() == true) {
                        relicRemoved = Save.state.relics[0];
                        relicRemoveIndex = 0;
                        Save.state.relics.RemoveAt(0);
                    }
                    Save.state.relics.Add(bestRelic.id);
                    //chanceOfOutcome = Evaluators.ChanceOfSpecificRelic(Save.state.character, Rarity.Boss);
                    //worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.TWO_RANDOM_UPGRADES: {
                    line1.Append("the upgrades");
                    //worstCaseValueProportion = 1f;
                    //chanceOfOutcome = 1f;
                    for (int a = 0; a < 2; a++) {
                        var priorUpgrades = 0;
                        Evaluators.ChooseBestAndWorstUpgrade(priorUpgrades, out var bestUpgrade, out var bestValue, out var worstUpgrade, out var worstValue);
                        if (bestValue <= 0) {
                            break;
                        }
                        var upgradeIndex = Save.state.cards.FirstIndexOf(x => x.id.Equals(bestUpgrade));
                        upgradeIndicies.Add(upgradeIndex);
                        Save.state.cards[upgradeIndex].upgrades++;
                        //worstCaseValueProportion *= worstValue / bestValue;
                        //chanceOfOutcome *= (2 - a) * 1f / Save.state.cards.Where(x => x.upgrades == 0).Count();
                    }
                    break;
                }
                case EventRewardElement.REMOVE_AND_UPGRADE: {
                    line1.Append("transforms");
                    var priorUpgrades = 0;
                    Evaluators.ChooseBestAndWorstUpgrade(priorUpgrades, out var bestUpgrade, out var bestValue, out var worstUpgrade, out var worstValue);
                    if (bestValue > 0) {
                        var upgradeIndex = Save.state.cards.FirstIndexOf(x => x.id.Equals(bestUpgrade));
                        upgradeIndicies.Add(upgradeIndex);
                        Save.state.cards[upgradeIndex].upgrades++;
                        //worstCaseValueProportion *= worstValue / bestValue;
                        // Adding one to denominator because card remove happens first in game
                        // Upgrading before remove because of dispose order
                        //chanceOfOutcome *= 1f / (Save.state.cards.Where(x => x.upgrades == 0).Count() + 1);
                    }

                    var removeIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[removeIndex]);
                    removedCardIndicies.Add(removeIndex);
                    line2.Append("Remove the " + Save.state.cards[removeIndex].name);
                    Save.state.cards.RemoveAt(removeIndex);
                    break;
                }
                case EventRewardElement.RELIC_CHANCE: {
                    // Scrap ooze
                    var chance = float.Parse(rewardValue.Substring(rewardValue.IndexOf(" ") + 1));
                    if (chance > 0f) {
                        Evaluators.ChooseBestRelic(out var bestRelic, out var bestValue, out var averageValue, out var count);
                        relics.Add(bestRelic);
                        Save.state.relics.Add(bestRelic);
                        //worstCaseValueProportion = 0f;
                        //averageCaseValueProportion = (averageValue / bestValue) * chance;
                        //chanceOfOutcome = (1f / count) * chance;
                    }
                    break;
                }
            }
            switch (cost) {
                case "NONE": {
                    break;
                }
                case "TEN_PERCENT_HP_LOSS": {
                    var maxHpLost = (int)((Save.state.max_health * .1f) + 1f - 0.00001f);
                    maxHealthLost += maxHpLost;
                    Save.state.max_health -= maxHpLost;
                    line1.Append(" for " + maxHealthLost + " max hp");
                    if (Save.state.max_health > Save.state.current_health) {
                        healthLost += Save.state.current_health - Save.state.max_health;
                        Save.state.current_health -= healthLost;
                    }
                    break;
                }
                case "NO_GOLD": {
                    goldAdded = -Save.state.gold;
                    Save.state.gold = 0;
                    line1.Append(" for the gold");
                    break;
                }
                case "CURSE": {
                    addedCardIndicies.Add(Save.state.AddCardById("Regret"));
                    line1.Append(" for the curse");
                    break;
                }
                case "PERCENT_DAMAGE": {
                    var damage = ((int)(Save.state.current_health * .1f)) * 3;
                    healthLost += damage;
                    Save.state.current_health -= damage;
                    line1.Append(" for the " + damage + " damage");
                    break;
                }
                default: {
                    var goldCost = int.Parse(cost);
                    goldAdded -= goldCost;
                    Save.state.gold -= goldCost;
                    line1.Append(" for " + goldCost);
                    break;
                }
            }
            if (!alreadyAdvised) {
                if (!line1.ToString().Equals("Take ")) {
                    description.Add(line1.ToString());
                }
                if (line2.Length > 0) {
                    description.Add(line2.ToString());
                }
            }
        }
        public void UpdateRewardPopulationStatistics(Evaluation eval) {
            if (statisticsGroup != null) {
                eval.RewardStats = statisticsGroup.Evaluate();
            }
        }
        public void Dispose() {
            foreach (var relic in relics) {
                Save.state.relics.Remove(relic);
            }
            foreach (var cardIndex in addedCardIndicies.OrderByDescending(x => x)) {
                Save.state.RemoveCardByIndex(cardIndex);
            }
            Save.state.gold -= goldAdded;
            foreach (var potionIndex in potionIndicies) {
                if (potionIndex != -1) {
                    Save.state.RemovePotion(potionIndex);
                }
            }
            if (tookGreenKey) {
                Save.state.has_emerald_key = false;
            }
            if (tookBlueKey) {
                Save.state.has_sapphire_key = false;
            }
            for (int i = cardsRemoved.Count - 1; i >= 0; i--) {
                var card = cardsRemoved[i];
                var index = removedCardIndicies[i];
                Save.state.cards.Insert(index, card);
            }
            Save.state.max_health += maxHealthLost;
            Save.state.current_health += healthLost;
            foreach (var upgradeIndex in upgradeIndicies) {
                Save.state.cards[upgradeIndex].upgrades--;
            }
            if (relicRemoved != null) {
                Save.state.relics.Insert(relicRemoveIndex, relicRemoved);
            }
            Save.state.addedBlockPerTurn = 0f;
            Save.state.addedDamagePerTurn = 0f;
            Save.state.addedSkill = false;
            if (bottled != null) {
                bottled.bottled = false;
            }
        }

        public bool IsValid() {
            var valid = true;
            valid &= Save.state.gold >= 0;
            valid &= !potionIndicies.Any(x => x == -1);
            valid &= !isInvalid;
            return valid;
        }
    }
}
