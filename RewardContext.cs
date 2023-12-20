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
        // If we have multiple possible rewards available, choose the best one, and a chanceOfOutCome proportionate to how likely that is
        public float chanceOfOutcome;
        public float worstCaseValueProportion = 1f;
        public int bonusCardRewards;
        public List<int> upgradeIndicies = new List<int>();
        public string relicRemoved = null;
        public int relicRemoveIndex = -1;
        public bool isInvalid;
        public Card bottled;
        public bool gainedMembershipCard;
        public RewardContext(List<RewardOption> rewardOptions, List<int> rewardIndicies, bool eligibleForBlueKey, bool isShop) {
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
                if (rewardGroup.advice != null && !string.IsNullOrEmpty(rewardGroup.advice[index])) {
                    description.Add(rewardGroup.advice[index]);
                }
                switch (rewardGroup.rewardType) {
                    case RewardType.Cards: {
                        var cardData = Database.instance.cardsDict[chosenId];
                        addedCardIndicies.Add(PumpernickelSaveState.instance.AddCardById(chosenId));
                        if (cardData.tags.TryGetValue(Tags.Damage.ToString(), out var damage)) {
                            Save.state.addedDamagePerTurn = damage / Save.state.cards.Count() * Evaluators.AverageCardsPerTurn();
                        }
                        if (cardData.tags.TryGetValue(Tags.Block.ToString(), out var block)) {
                            Save.state.addedBlockPerTurn = block / Save.state.cards.Count() * Evaluators.AverageCardsPerTurn();
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
                        description.Add("Take the " + chosen);
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
                        var removeIndex = Evaluators.CardRemoveTarget();
                        cardsRemoved.Add(Save.state.cards[removeIndex]);
                        removedCardIndicies.Add(removeIndex);
                        description.Add("Remove the " + Save.state.cards[removeIndex].name);
                        Save.state.cards.RemoveAt(removeIndex);
                        break;
                    }
                    case RewardType.Event: {
                        var eventCost = rewardGroup.eventCost[index];
                        HandleEvent(Enum.GetValues<EventRewardElement>().Where(x => x.ToString().Equals(chosen)).Single(), eventCost);
                        break;
                    }
                    default: {
                        throw new System.NotImplementedException();
                    }
                }
            }
        }

        protected void HandleEvent(EventRewardElement reward, string cost) {
            switch (cost) {
                case "NONE": {
                    break;
                }
                case "TEN_PERCENT_HP_LOSS": {
                    var maxHpLost = (int)((Save.state.max_health * .1f) + 1f - 0.00001f);
                    maxHealthLost += maxHpLost;
                    Save.state.max_health -= maxHpLost;
                    if (Save.state.max_health > Save.state.current_health) {
                        healthLost += Save.state.current_health - Save.state.max_health;
                        Save.state.current_health -= healthLost;
                    }
                    break;
                }
                case "NO_GOLD": {
                    goldAdded = -Save.state.gold;
                    Save.state.gold = 0;
                    break;
                }
                case "CURSE": {
                    addedCardIndicies.Add(PumpernickelSaveState.instance.AddCardById("Shame"));
                    break;
                }
                case "PERCENT_DAMAGE": {
                    var damage = ((int)(Save.state.current_health * .1f)) * 3;
                    healthLost += damage;
                    Save.state.current_health -= damage;
                    break;
                }
                default: {
                    var goldCost = int.Parse(cost);
                    goldAdded -= goldCost;
                    Save.state.gold -= goldCost;
                    break;
                }
            }
            switch (reward) {
                case EventRewardElement.RANDOM_COLORLESS_2: {
                    description.Add("Take the rare colorless");
                    Evaluators.BestAndWorstRewardResults(Color.Colorless, out var bestCard, out var bestValue, out float worstCaseValue, Rarity.Rare);
                    addedCardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    chanceOfOutcome = Evaluators.ChanceOfSpecificCardInReward(Color.Colorless, Rarity.Rare, 1f);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.THREE_CARDS: {
                    description.Add("Take the card reward");
                    bonusCardRewards += 1;
                    break;
                }
                case EventRewardElement.ONE_RANDOM_RARE_CARD: {
                    description.Add("Take the random rare card");
                    Evaluators.BestAndWorstRewardResults(Save.state.character.ToColor(), out var bestCard, out var bestValue, out float worstCaseValue, Rarity.Rare);
                    addedCardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    chanceOfOutcome = Evaluators.ChanceOfSpecificCard(Save.state.character.ToColor(), Rarity.Rare);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.REMOVE_CARD: {
                    var removeIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[removeIndex]);
                    removedCardIndicies.Add(removeIndex);
                    description.Add("Remove the " + Save.state.cards[removeIndex].name);
                    Save.state.cards.RemoveAt(removeIndex);
                    break;
                }
                case EventRewardElement.UPGRADE_CARD: {
                    var bestUpgrade = Evaluators.ChooseBestUpgrade(out var _);
                    var upgradeIndex = Save.state.cards.FirstIndexOf(x => x.id.Equals(bestUpgrade));
                    upgradeIndicies.Add(upgradeIndex);
                    Save.state.cards[upgradeIndex].upgrades++;
                    description.Add("Upgrade the " + Save.state.cards[upgradeIndex].name);
                    break;
                }
                case EventRewardElement.RANDOM_COLORLESS: {
                    description.Add("Take the colorless card reward");
                    Evaluators.BestAndWorstRewardResults(Color.Colorless, out var bestCard, out var bestValue, out float worstCaseValue, Rarity.Uncommon);
                    addedCardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    chanceOfOutcome = Evaluators.ChanceOfSpecificCard(Color.Colorless, Rarity.Uncommon);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.TRANSFORM_CARD: {
                    var removeIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[removeIndex]);
                    removedCardIndicies.Add(removeIndex);
                    description.Add("Transform the " + Save.state.cards[removeIndex].name);
                    Save.state.cards.RemoveAt(removeIndex);

                    Evaluators.AverageTransformValue(out var averageCardId);
                    addedCardIndicies.Add(PumpernickelSaveState.instance.AddCardById(averageCardId));
                    break;
                }
                case EventRewardElement.THREE_SMALL_POTIONS: {
                    description.Add("Take the potions");
                    // TODO: this
                    break;
                }
                case EventRewardElement.RANDOM_COMMON_RELIC: {
                    description.Add("Take the random common relic");
                    Evaluators.RandomRelicValue(Save.state.character, out var bestRelic, out var bestValue, out float worstCaseValue, Rarity.Common);
                    relics.Add(bestRelic.id);
                    Save.state.relics.Add(bestRelic.id);
                    chanceOfOutcome = Evaluators.ChanceOfSpecificRelic(Save.state.character, Rarity.Common);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.TEN_PERCENT_HP_BONUS: {
                    var bonus = (int)(Save.state.max_health * .1f);
                    description.Add("Take the " + bonus + " max hp");
                    maxHealthLost -= bonus;
                    healthLost -= bonus;
                    Save.state.max_health += bonus;
                    Save.state.current_health += bonus;
                    break;
                }
                case EventRewardElement.HUNDRED_GOLD: {
                    description.Add("Take the hundred gold");
                    goldAdded = 100;
                    Save.state.gold += 100;
                    break;
                }
                case EventRewardElement.THREE_ENEMY_KILL: {
                    description.Add("Take the lament");
                    relics.Add("NeowsBlessing");
                    Save.state.relics.Add("NeowsBlessing");
                    break;
                }
                case EventRewardElement.REMOVE_TWO: {
                    var desc = new StringBuilder("Remove the ");
                    var firstRemoveIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[firstRemoveIndex]);
                    desc.Append(Save.state.cards[firstRemoveIndex].name + " and the ");
                    removedCardIndicies.Add(firstRemoveIndex);
                    Save.state.cards.RemoveAt(firstRemoveIndex);
                    var secondRemoveIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[secondRemoveIndex]);
                    removedCardIndicies.Add(secondRemoveIndex);
                    Save.state.cards.RemoveAt(secondRemoveIndex);
                    desc.Append(Save.state.cards[secondRemoveIndex].name);
                    description.Add(desc.ToString());
                    break;
                }
                case EventRewardElement.TRANSFORM_TWO_CARDS: {
                    Evaluators.AverageTransformValue(out var averageCard);

                    var desc = new StringBuilder("Transform the ");
                    var firstRemoveIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[firstRemoveIndex]);
                    desc.Append(Save.state.cards[firstRemoveIndex].name + " and the ");
                    removedCardIndicies.Add(firstRemoveIndex);
                    Save.state.cards.RemoveAt(firstRemoveIndex);
                    var secondRemoveIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[secondRemoveIndex]);
                    removedCardIndicies.Add(secondRemoveIndex);
                    Save.state.cards.RemoveAt(secondRemoveIndex);
                    desc.Append(Save.state.cards[secondRemoveIndex].name);
                    description.Add(desc.ToString());

                    addedCardIndicies.Add(PumpernickelSaveState.instance.AddCardById(averageCard));
                    addedCardIndicies.Add(PumpernickelSaveState.instance.AddCardById(averageCard));
                    break;
                }
                case EventRewardElement.ONE_RARE_RELIC: {
                    description.Add("Take the rare relic");
                    Evaluators.RandomRelicValue(Save.state.character, out var bestRelic, out var bestValue, out float worstCaseValue, Rarity.Rare);
                    relics.Add(bestRelic.id);
                    Save.state.relics.Add(bestRelic.id);
                    chanceOfOutcome = Evaluators.ChanceOfSpecificRelic(Save.state.character, Rarity.Rare);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.THREE_RARE_CARDS: {
                    description.Add("Take the rare card reward");
                    Evaluators.BestAndWorstRewardResults(Save.state.character.ToColor(), out var bestCard, out var bestValue, out float worstCaseValue, Rarity.Rare);
                    addedCardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    chanceOfOutcome = Evaluators.ChanceOfSpecificCardInReward(Save.state.character.ToColor(), Rarity.Rare, 1f);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.TWO_FIFTY_GOLD: {
                    description.Add("Take the 250 gold");
                    goldAdded = 250;
                    Save.state.gold += 250;
                    break;
                }
                case EventRewardElement.TWENTY_PERCENT_HP_BONUS: {
                    var bonus = ((int)(Save.state.max_health * .1f)) * 2;
                    description.Add("Take the " + bonus + " max hp");
                    maxHealthLost -= bonus;
                    healthLost -= bonus;
                    Save.state.max_health += bonus;
                    Save.state.current_health += bonus;
                    break;
                }
                case EventRewardElement.BOSS_RELIC: {
                    description.Add("Do a boss relic swap");
                    Evaluators.RandomRelicValue(Save.state.character, out var bestRelic, out var bestValue, out float worstCaseValue, Rarity.Boss);
                    relics.Add(bestRelic.id);
                    relicRemoved = Save.state.relics[0];
                    relicRemoveIndex = 0;
                    Save.state.relics.RemoveAt(0);
                    Save.state.relics.Add(bestRelic.id);
                    chanceOfOutcome = Evaluators.ChanceOfSpecificRelic(Save.state.character, Rarity.Boss);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case EventRewardElement.TWO_RANDOM_UPGRADES: {
                    worstCaseValueProportion = 1f;
                    chanceOfOutcome = 1f;
                    for (int a = 0; a < 2; a++) {
                        var priorUpgrades = 0;
                        Evaluators.ChooseBestAndWorstUpgrade(priorUpgrades, out var bestUpgrade, out var bestValue, out var worstUpgrade, out var worstValue);
                        if (bestValue <= 0) {
                            break;
                        }
                        var upgradeIndex = Save.state.cards.FirstIndexOf(x => x.id.Equals(bestUpgrade));
                        upgradeIndicies.Add(upgradeIndex);
                        Save.state.cards[upgradeIndex].upgrades++;
                        worstCaseValueProportion *= worstValue / bestValue;
                        chanceOfOutcome *= (2 - a) * 1f / Save.state.cards.Where(x => x.upgrades == 0).Count();
                    }
                    break;
                }
                case EventRewardElement.REMOVE_AND_UPGRADE: {
                    var priorUpgrades = 0;
                    Evaluators.ChooseBestAndWorstUpgrade(priorUpgrades, out var bestUpgrade, out var bestValue, out var worstUpgrade, out var worstValue);
                    if (bestValue > 0) {
                        var upgradeIndex = Save.state.cards.FirstIndexOf(x => x.id.Equals(bestUpgrade));
                        upgradeIndicies.Add(upgradeIndex);
                        Save.state.cards[upgradeIndex].upgrades++;
                        worstCaseValueProportion *= worstValue / bestValue;
                        // Adding one to denominator because card remove happens first in game
                        // Upgrading before remove because of dispose order
                        chanceOfOutcome *= 1f / (Save.state.cards.Where(x => x.upgrades == 0).Count() + 1);
                    }

                    var removeIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[removeIndex]);
                    removedCardIndicies.Add(removeIndex);
                    description.Add("Remove the " + Save.state.cards[removeIndex].name);
                    Save.state.cards.RemoveAt(removeIndex);
                    break;
                }
            }
        }

        public void Dispose() {
            foreach (var relic in relics) {
                Save.state.relics.Remove(relic);
            }
            foreach (var cardIndex in addedCardIndicies.OrderByDescending(x => x)) {
                PumpernickelSaveState.instance.RemoveCardByIndex(cardIndex);
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
