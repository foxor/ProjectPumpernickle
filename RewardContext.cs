using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public bool[] needsMoreInfo;
        public static RewardOption BuildEvent(params RewardOptionPart[] parts) {
            RewardOption r = new RewardOption();
            r.values = parts.Select(x => x.value ?? "None").ToArray();
            r.eventCost = parts.Select(x => string.IsNullOrEmpty(x.eventCost) ? "NONE" : x.eventCost).ToArray();
            r.hpCost = parts.Select(x => x.hpCost).ToArray();
            r.advice = parts.Select(x => x.advice).ToArray();
            r.needsMoreInfo = parts.Select(x => x.needsMoreInfo).ToArray();
            r.rewardType = RewardType.Event;
            r.skippable = false;
            return r;
        }
        public static RewardOption Build(IEnumerable<RewardOptionPart> parts) {
            RewardOption r = new RewardOption();
            r.values = parts.Select(x => x.value ?? "").ToArray();
            r.eventCost = parts.Select(x => x.eventCost).ToArray();
            r.hpCost = parts.Select(x => x.hpCost).ToArray();
            r.advice = parts.Select(x => x.advice).ToArray();
            r.needsMoreInfo = parts.Select(x => x.needsMoreInfo).ToArray();
            return r;
        }
    }
    public struct RewardOptionPart {
        public string value;
        public string eventCost;
        public int hpCost;
        public string advice;
        public bool needsMoreInfo;
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
        public List<int> upgradeIndicies = new List<int>();
        public string relicRemoved = null;
        public int relicRemoveIndex = -1;
        public bool isInvalid;
        public Card bottled;
        public bool gainedMembershipCard;
        public IRewardStatisticsGroup statisticsGroup;
        public bool needsMoreInfo;
        public RewardContext(in List<RewardOption> rewardOptions, List<int> rewardIndicies, bool eligibleForBlueKey, bool isShop, int upgradeIndex) {
            if (upgradeIndex != -1) {
                var cardIndex = Path.plausibleUpgrades[upgradeIndex];
                Save.state.cards[cardIndex].upgrades++;
                Save.state.cards[cardIndex].ParseDescription();
                upgradeIndicies.Add(cardIndex);
            }
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
                if (rewardGroup.needsMoreInfo != null) {
                    needsMoreInfo |= rewardGroup.needsMoreInfo[index];
                }
                switch (rewardGroup.rewardType) {
                    case RewardType.Cards: {
                        var cardData = Database.instance.cardsDict[chosenId];
                        addedCardIndicies.Add(Save.state.AddCardById(chosen));
                        if (cardData.tags.TryGetValue(Tags.Damage.ToString(), out var damage)) {
                            Save.state.addedDamagePerTurn = damage / Save.state.cards.Count() * Evaluators.AverageCardsPlayedPerTurn();
                        }
                        if (cardData.tags.TryGetValue(Tags.Block.ToString(), out var block)) {
                            Save.state.addedBlockPerTurn = block / Save.state.cards.Count() * Evaluators.AverageCardsPlayedPerTurn();
                        }
                        Save.state.AddChoosingNow(chosenId);
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
                        var relicId = chosen;
                        var parameters = "";
                        var parameterStartIndex = relicId.IndexOf(":");
                        if (parameterStartIndex >= 0) {
                            parameters = relicId.Substring(parameterStartIndex + 1);
                            relicId = relicId.Substring(0, parameterStartIndex);
                        }
                        relics.Add(relicId);
                        var pickFn = EvaluationFunctionReflection.GetRelicOnPickedFunctionCached(relicId);
                        Save.state.relics.Add(relicId);
                        Save.state.relic_counters.Add(0);
                        description.Add("Take the " + Database.instance.relicsDict[relicId].name);
                        pickFn(parameters, this);
                        if (relicId.Equals("Membership Card")) {
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
                        HandleEvent(Enum.GetValues<EventRewardElement>().Where(x => x.ToString().Equals(rewardHeader)).Single(), chosen, eventCost);
                        break;
                    }
                    default: {
                        throw new System.NotImplementedException();
                    }
                }
            }
        }

        public void HandleEvent(EventRewardElement reward, string rewardValue, string cost) {
            switch (reward) {
                case EventRewardElement.RANDOM_COLORLESS_2: {
                    var stats = new AddCardStatisticsGroup(Color.Colorless, Rarity.Rare);
                    statisticsGroup = stats;
                    addedCardIndicies.Add(Save.state.AddCardById(stats.cardId));
                    break;
                }
                case EventRewardElement.THREE_CARDS: {
                    var stats = new ChooseCardsStatisticsGroup();
                    statisticsGroup = stats;
                    addedCardIndicies.Add(Save.state.AddCardById(stats.cardId));
                    break;
                }
                case EventRewardElement.ONE_RANDOM_RARE_CARD: {
                    var stats = new AddCardStatisticsGroup(Save.state.character.ToColor(), Rarity.Rare);
                    statisticsGroup = stats;
                    addedCardIndicies.Add(Save.state.AddCardById(stats.cardId));
                    break;
                }
                case EventRewardElement.REMOVE_CARD: {
                    var removeIndex = int.Parse(rewardValue.Substring(rewardValue.IndexOf(" ") + 1));
                    cardsRemoved.Add(Save.state.cards[removeIndex]);
                    removedCardIndicies.Add(removeIndex);
                    Save.state.cards.RemoveAt(removeIndex);
                    break;
                }
                case EventRewardElement.UPGRADE_CARD: {
                    var upgradeIndex = int.Parse(rewardValue.Substring(rewardValue.IndexOf(" ") + 1));
                    upgradeIndicies.Add(upgradeIndex);
                    Save.state.cards[upgradeIndex].upgrades++;
                    break;
                }
                case EventRewardElement.RANDOM_COLORLESS: {
                    var stats = new ChooseCardsStatisticsGroup(new float[]{ 0f, 3f, 0f }, color: Color.Colorless);
                    statisticsGroup = stats;
                    addedCardIndicies.Add(Save.state.AddCardById(stats.cardId));
                    break;
                }
                case EventRewardElement.TRANSFORM_CARD: {
                    var removeIndex = int.Parse(rewardValue.Substring(rewardValue.IndexOf(" ") + 1));
                    var card = Save.state.cards[removeIndex];
                    cardsRemoved.Add(card);
                    removedCardIndicies.Add(removeIndex);
                    Save.state.cards.RemoveAt(removeIndex);

                    var stats = new AddCardStatisticsGroup(card.cardColor, Rarity.Randomable);
                    statisticsGroup = stats;
                    addedCardIndicies.Add(Save.state.AddCardById(stats.cardId));
                    needsMoreInfo = true;
                    break;
                }
                case EventRewardElement.THREE_SMALL_POTIONS: {
                    // TODO: this
                    break;
                }
                case EventRewardElement.RANDOM_COMMON_RELIC: {
                    statisticsGroup = new AddCommonRelicStatisicsGroup();
                    relics.Add(AddCommonRelicStatisicsGroup.ASSUMED_ADD);
                    Save.state.relics.Add(AddCommonRelicStatisicsGroup.ASSUMED_ADD);
                    Save.state.relic_counters.Add(0);
                    break;
                }
                case EventRewardElement.TEN_PERCENT_HP_BONUS: {
                    var bonus = (int)(Save.state.max_health * .1f);
                    maxHealthLost -= bonus;
                    healthLost -= bonus;
                    Save.state.max_health += bonus;
                    Save.state.current_health += bonus;
                    break;
                }
                case EventRewardElement.HUNDRED_GOLD: {
                    goldAdded = 100;
                    Save.state.gold += 100;
                    break;
                }
                case EventRewardElement.THREE_ENEMY_KILL: {
                    relics.Add("NeowsBlessing");
                    Save.state.relics.Add("NeowsBlessing");
                    Save.state.relic_counters.Add(0);
                    break;
                }
                case EventRewardElement.REMOVE_TWO: {
                    /*var firstRemoveIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[firstRemoveIndex]);
                    removedCardIndicies.Add(firstRemoveIndex);
                    Save.state.cards.RemoveAt(firstRemoveIndex);
                    var secondRemoveIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[secondRemoveIndex]);
                    removedCardIndicies.Add(secondRemoveIndex);
                    Save.state.cards.RemoveAt(secondRemoveIndex);
                    line2.Append(Save.state.cards[secondRemoveIndex].name);*/
                    break;
                }
                case EventRewardElement.TRANSFORM_TWO_CARDS: {
                    /*line1.Append("transforms");
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
                    //addedCardIndicies.Add(Save.state.AddCardById(averageCard));*/
                    break;
                }
                case EventRewardElement.ONE_RARE_RELIC: {
                    relics.Add(AddRareRelicStatisicsGroup.ASSUMED_ADD);
                    Save.state.relic_counters.Add(0);
                    Save.state.relics.Add(AddRareRelicStatisicsGroup.ASSUMED_ADD);
                    statisticsGroup = new AddRareRelicStatisicsGroup();
                    break;
                }
                case EventRewardElement.THREE_RARE_CARDS: {
                    var stats = new ChooseCardsStatisticsGroup(new float[]{ 0f, 0f, 3f }, null, Save.state.character.ToColor());
                    statisticsGroup = stats;
                    addedCardIndicies.Add(Save.state.AddCardById(stats.cardId));
                    needsMoreInfo = true;
                    break;
                }
                case EventRewardElement.TWO_FIFTY_GOLD: {
                    goldAdded = 250;
                    Save.state.gold += 250;
                    break;
                }
                case EventRewardElement.TWENTY_PERCENT_HP_BONUS: {
                    var bonus = ((int)(Save.state.max_health * .1f)) * 2;
                    maxHealthLost -= bonus;
                    healthLost -= bonus;
                    Save.state.max_health += bonus;
                    Save.state.current_health += bonus;
                    break;
                }
                case EventRewardElement.BOSS_RELIC: {
                    relics.Add(BossSwapStatisicsGroup.ASSUMED_SWAP);
                    relicRemoved = Save.state.relics[0];
                    relicRemoveIndex = 0;
                    Save.state.relics.RemoveAt(0);
                    Save.state.relics.Add(BossSwapStatisicsGroup.ASSUMED_SWAP);
                    statisticsGroup = new BossSwapStatisicsGroup();
                    break;
                }
                case EventRewardElement.TWO_RANDOM_UPGRADES: {
                    var stats = new RandomUpgradeStatisticsGroup(2);
                    statisticsGroup = stats;
                    foreach (var updateIndex in stats.assumedUpgrades) {
                        Save.state.cards[updateIndex].upgrades++;
                    }
                    upgradeIndicies = stats.assumedUpgrades;
                    break;
                }
                case EventRewardElement.REMOVE_AND_UPGRADE: {
                    // Designer in spire
                    /*var priorUpgrades = 0;
                    Evaluators.ChooseBestAndWorstUpgrade(priorUpgrades, 0f, out var bestUpgrade, out var bestValue, out var worstUpgrade, out var worstValue);
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
                    Save.state.cards.RemoveAt(removeIndex);*/
                    break;
                }
                case EventRewardElement.RELIC_CHANCE: {
                    var chance = float.Parse(rewardValue.Substring(rewardValue.IndexOf(" ") + 1));
                    if (chance > 0f) {
                        var stats = new AddRelicsStatisticsGroup(chance: chance);
                        relics.Add(stats.relicId);
                        Save.state.relics.Add(stats.relicId);
                        statisticsGroup = stats;
                    }
                    break;
                }
                case EventRewardElement.CURSED_TOME: {
                    statisticsGroup = new CursedTomeRewardGroup();
                    relics.Add(CursedTomeRewardGroup.CHOSEN);
                    Save.state.relics.Add(CursedTomeRewardGroup.CHOSEN);
                    Save.state.relic_counters.Add(0);
                    break;
                }
                case EventRewardElement.HEAL: {
                    var heal = int.Parse(rewardValue.Substring(rewardValue.IndexOf(" ") + 1));
                    Save.state.current_health += heal;
                    healthLost = -heal;
                    break;
                }
                case EventRewardElement.MAX_HP: {
                    var hp = int.Parse(rewardValue.Substring(rewardValue.IndexOf(" ") + 1));
                    Save.state.max_health += hp;
                    Save.state.current_health += hp;
                    maxHealthLost = -hp;
                    healthLost = -hp;
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
                    //addedCardIndicies.Add(Save.state.AddCardById("Regret"));
                    break;
                }
                case "REGRET": {
                    addedCardIndicies.Add(Save.state.AddCardById("Regret"));
                    break;
                }
                case "SHAME": {
                    addedCardIndicies.Add(Save.state.AddCardById("Shame"));
                    break;
                }
                case "DECAY": {
                    addedCardIndicies.Add(Save.state.AddCardById("Decay"));
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
        }
        public void UpdateRewardPopulationStatistics(Evaluation eval) {
            if (statisticsGroup != null) {
                eval.RewardStats = statisticsGroup.Evaluate();
            }
        }
        public float rewardPowerOffset {
            get {
                var totalPower = 0f;
                totalPower += upgradeIndicies.Select(x => Evaluators.UpgradePowerGutFeeling(x) - 1f).Sum();
                return totalPower;
            }
        }
        public void Dispose() {
            foreach (var relic in relics) {
                var index = Save.state.relics.IndexOf(relic);
                Save.state.relics.RemoveAt(index);
                Save.state.relic_counters.RemoveAt(index);
            }
            for (int i = 0; i < addedCardIndicies.Count; i++) {
                var index = addedCardIndicies[i];
                Save.state.RemoveCardByIndex(index);
                for (int j = i + 1; j < addedCardIndicies.Count; j++) {
                    if (addedCardIndicies[j] >= index) {
                        addedCardIndicies[j]--;
                    }
                }
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
