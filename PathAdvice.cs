using Accessibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace ProjectPumpernickle {
    public enum ScoreReason : byte {
        ActSurvival,
        UpgradeCount,
        RelicCount,
        CardReward,
        GoldGained,
        Key,
        CurrentEffectiveHealth,
        BringGoldToShop,
        MissingKey,
        DeckQuality,
        RelicQuality,
        AvoidCard,
        AvoidDuplicateCards,
        DeckSizeLimit,
        MissingCards,
        MissingComboPieces,
        SmallDeck,
        LargeDeck,
        Variance,
        COUNT,
    }
    public class  Evaluation {
        public static Evaluation Active;
        private float[] Scores = new float[(byte)ScoreReason.COUNT];
        public List<string> Advice = new List<string>();
        public Path Path = null;
        public bool NeedsMoreInfo;
        public float RewardVariance;
        public float WorstCaseRewardFactor;
        public int BonusCardRewards;

        public Evaluation() {
            Active = this;
            Save.state.earliestInfinite = 0;
            Save.state.buildingInfinite = false;
            Save.state.expectingToRedBlue = Save.state.character == PlayerCharacter.Watcher;
            Save.state.huntingCards.Clear();
        }

        public override string ToString() {
            return string.Join("\r\n", Advice);
        }

        public void AddScore(ScoreReason reason, float delta) {
            Scores[(byte)reason] += delta;
        }

        public void MergeScores(float[] scores) {
            for (int i = 0; i < (byte)ScoreReason.COUNT; i++) {
                Scores[i] += scores[i];
            }
        }

        public float Score {
            get {
                return Scores.Sum();
            }
        }
    }
    public class RewardOption {
        public RewardType rewardType;
        public string[] values;
        public string[] neowCost;
        public int cost;
        public bool skippable = true;
    }
    public class RewardContext : IDisposable {
        public List<string> relics = new List<string>();
        public List<int> cardIndicies = new List<int>();
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
        public RewardContext(List<RewardOption> rewardOptions, List<int> rewardIndicies, bool eligibleForBlueKey) {
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
                    else {
                        description.Add("Skip the " + rewardGroup.rewardType);
                    }
                    if (!rewardGroup.skippable) {
                        isInvalid = true;
                    }
                    continue;
                }
                var chosen = rewardGroup.values[index];
                var chosenId = chosen.Replace("+", "");
                Save.state.gold -= rewardGroup.cost;
                goldAdded -= rewardGroup.cost;
                switch (rewardGroup.rewardType) {
                    case RewardType.Cards: {
                        cardIndicies.Add(PumpernickelSaveState.instance.AddCardById(chosenId));
                        description.Add("Take the " + Database.instance.cardsDict[chosenId].name);
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
                        // TODO: some relics require more advice when they are chosen, like bottles, empty cage and astolabe
                        relics.Add(chosen);
                        Save.state.relics.Add(chosen);
                        description.Add("Take the " + chosen);
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
                    case RewardType.Neow: {
                        var cost = rewardGroup.neowCost[index];
                        HandleNeowReward(chosen, cost);
                        break;
                    }
                    default: {
                        throw new System.NotImplementedException();
                    }
                }
            }
        }

        protected void HandleNeowReward(string reward, string cost) {
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
                    cardIndicies.Add(PumpernickelSaveState.instance.AddCardById("Shame"));
                    break;
                }
                case "PERCENT_DAMAGE": {
                    var damage = ((int)(Save.state.current_health * .1f)) * 3;
                    healthLost += damage;
                    Save.state.current_health -= damage;
                    break;
                }
            }
            switch (reward) {
                case "RANDOM_COLORLESS_2": {
                    description.Add("Take the rare colorless");
                    Evaluators.RandomCardValue(Color.Colorless, out var bestCard, out var bestValue, out float worstCaseValue, Rarity.Rare);
                    cardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    chanceOfOutcome = Evaluators.ChanceOfSpecificCardInReward(Color.Colorless, Rarity.Rare, 1f);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case "THREE_CARDS": {
                    description.Add("Take the card reward");
                    bonusCardRewards += 1;
                    break;
                }
                case "ONE_RANDOM_RARE_CARD": {
                    description.Add("Take the random rare card");
                    Evaluators.RandomCardValue(Save.state.character.ToColor(), out var bestCard, out var bestValue, out float worstCaseValue, Rarity.Rare);
                    cardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    chanceOfOutcome = Evaluators.ChanceOfSpecificCard(Save.state.character.ToColor(), Rarity.Rare);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case "REMOVE_CARD": {
                    var removeIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[removeIndex]);
                    removedCardIndicies.Add(removeIndex);
                    description.Add("Remove the " + Save.state.cards[removeIndex].name);
                    Save.state.cards.RemoveAt(removeIndex);
                    break;
                }
                case "UPGRADE_CARD": {
                    var bestUpgrade = Evaluators.ChooseBestUpgrade();
                    var upgradeIndex = Save.state.cards.FirstIndexOf(x => x.id.Equals(bestUpgrade));
                    upgradeIndicies.Add(upgradeIndex);
                    Save.state.cards[upgradeIndex].upgrades++;
                    description.Add("Upgrade the " + Save.state.cards[upgradeIndex].name);
                    break;
                }
                case "RANDOM_COLORLESS": {
                    description.Add("Take the colorless card reward");
                    Evaluators.RandomCardValue(Color.Colorless, out var bestCard, out var bestValue, out float worstCaseValue, Rarity.Uncommon);
                    cardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    chanceOfOutcome = Evaluators.ChanceOfSpecificCard(Color.Colorless, Rarity.Uncommon);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case "TRANSFORM_CARD": {
                    var removeIndex = Evaluators.CardRemoveTarget();
                    cardsRemoved.Add(Save.state.cards[removeIndex]);
                    removedCardIndicies.Add(removeIndex);
                    description.Add("Transform the " + Save.state.cards[removeIndex].name);
                    Save.state.cards.RemoveAt(removeIndex);

                    Evaluators.RandomCardValue(Save.state.character.ToColor(), out var bestCard, out var bestValue, out float worstCaseValue);
                    cardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    chanceOfOutcome = Evaluators.ChanceOfSpecificCard(Save.state.character.ToColor(), Rarity.Rare);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case "THREE_SMALL_POTIONS": {
                    description.Add("Take the potions");
                    // TODO: this
                    break;
                }
                case "RANDOM_COMMON_RELIC": {
                    description.Add("Take the random common relic");
                    Evaluators.RandomRelicValue(Save.state.character, out var bestRelic, out var bestValue, out float worstCaseValue, Rarity.Common);
                    relics.Add(bestRelic.id);
                    Save.state.relics.Add(bestRelic.id);
                    chanceOfOutcome = Evaluators.ChanceOfSpecificRelic(Save.state.character, Rarity.Common);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case "TEN_PERCENT_HP_BONUS": {
                    var bonus = (int)(Save.state.max_health * .1f);
                    description.Add("Take the " + bonus + " max hp");
                    maxHealthLost -= bonus;
                    healthLost -= bonus;
                    Save.state.max_health += bonus;
                    Save.state.current_health += bonus;
                    break;
                }
                case "HUNDRED_GOLD": {
                    description.Add("Take the hundred gold");
                    goldAdded = 100;
                    Save.state.gold += 100;
                    break;
                }
                case "THREE_ENEMY_KILL": {
                    description.Add("Take the lament");
                    relics.Add("NeowsBlessing");
                    Save.state.relics.Add("NeowsBlessing");
                    break;
                }
                case "REMOVE_TWO": {
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
                case "TRANSFORM_TWO_CARDS": {
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

                    Evaluators.RandomCardValue(Save.state.character.ToColor(), out var bestCard, out var bestValue, out float worstCaseValue);
                    cardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    cardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    // This is not really the chance here, but transform 2 is really good, so giving it a pseudo-math boost
                    chanceOfOutcome = Evaluators.ChanceOfSpecificCard(Save.state.character.ToColor(), Rarity.Rare) / 2f;
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case "ONE_RARE_RELIC": {
                    description.Add("Take the rare relic");
                    Evaluators.RandomRelicValue(Save.state.character, out var bestRelic, out var bestValue, out float worstCaseValue, Rarity.Rare);
                    relics.Add(bestRelic.id);
                    Save.state.relics.Add(bestRelic.id);
                    chanceOfOutcome = Evaluators.ChanceOfSpecificRelic(Save.state.character, Rarity.Rare);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case "THREE_RARE_CARDS": {
                    description.Add("Take the rare card reward");
                    Evaluators.RandomCardValue(Save.state.character.ToColor(), out var bestCard, out var bestValue, out float worstCaseValue, Rarity.Rare);
                    cardIndicies.Add(PumpernickelSaveState.instance.AddCardById(bestCard.id));
                    chanceOfOutcome = Evaluators.ChanceOfSpecificCardInReward(Save.state.character.ToColor(), Rarity.Rare, 1f);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
                case "TWO_FIFTY_GOLD": {
                    description.Add("Take the 250 gold");
                    goldAdded = 250;
                    Save.state.gold += 250;
                    break;
                }
                case "TWENTY_PERCENT_HP_BONUS": {
                    var bonus = ((int)(Save.state.max_health * .1f)) * 2;
                    description.Add("Take the " + bonus + " max hp");
                    maxHealthLost -= bonus;
                    healthLost -= bonus;
                    Save.state.max_health += bonus;
                    Save.state.current_health += bonus;
                    break;
                }
                case "BOSS_RELIC": {
                    description.Add("Do a boss relic swap");
                    Evaluators.RandomRelicValue(Save.state.character, out var bestRelic, out var bestValue, out float worstCaseValue, Rarity.Boss);
                    relics.Add(bestRelic.id);
                    relicRemoved = Save.state.relics[0];
                    relicRemoveIndex = 0;
                    Save.state.relics.Add(bestRelic.id);
                    chanceOfOutcome = Evaluators.ChanceOfSpecificRelic(Save.state.character, Rarity.Boss);
                    worstCaseValueProportion = worstCaseValue / bestValue;
                    break;
                }
            }
        }

        public void Dispose() {
            foreach (var relic in relics) {
                Save.state.relics.Remove(relic);
            }
            foreach (var cardIndex in cardIndicies.OrderByDescending(x => x)) {
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
        }

        public bool IsValid() {
            var valid = true;
            valid &= Save.state.gold >= 0;
            valid &= !potionIndicies.Any(x => x == -1);
            valid &= !isInvalid;
            return valid;
        }
    }
    public class PathAdvice {
        protected static IEnumerable<Evaluation> MultiplexRewards(List<RewardOption> rewardOptions, bool eligibleForBlueKey) {
            var totalOptions = rewardOptions.Select(x => x.values.Length + 1).Aggregate(1, (a, x) => a * x);
            List<int> rewardIndicies = new List<int>();
            for (int i = 0; i < totalOptions; i++) {
                rewardIndicies.Clear();
                int residual = i;
                for (int j = 0; j < rewardOptions.Count; j++) {
                    var optionCount = rewardOptions[j].values.Length + 1;
                    rewardIndicies.Add(residual % optionCount);
                    residual /= optionCount;
                }
                using (var context = new RewardContext(rewardOptions, rewardIndicies, eligibleForBlueKey)) {
                    if (!context.IsValid()) {
                        continue;
                    }
                    var evaluation = new Evaluation() {
                        Advice = context.description,
                        RewardVariance = context.chanceOfOutcome,
                        WorstCaseRewardFactor = context.worstCaseValueProportion,
                        BonusCardRewards = context.bonusCardRewards,
                    };
                    Evaluate(evaluation);
                    yield return evaluation;
                }
            }
        }

        public static float ExpectedCardRemovesAvailable(Path path) {
            // TODO
            return 5f;
        }

        public static float CardRemovePoints(Path path) {
            if (Save.state.character == PlayerCharacter.Watcher) {
                var anticipatedEndGameDeckSize = Evaluators.PermanentDeckSize() - ExpectedCardRemovesAvailable(path) + (Evaluators.HasCalmEnter() ? 0 : 1);
                if (anticipatedEndGameDeckSize > 8) {
                    return .2f;
                }
                // Allocate 20 pumpernickel points to getting 5 removes on watcher going for infinite
                return path.ExpectedPossibleCardRemoves() / 5f * 20f;
            }
            // TODO
            return .2f;
        }

        public static void DamageStatsPerCardReward(int byTurn, float cardsInDeck, out float damage, out float cost) {
            // Assumes unupgraded
            switch (Save.state.character) {
                case PlayerCharacter.Watcher: {
                    break;
                }
                default: {
                    throw new NotImplementedException();
                }
            }
            damage = 0; cost = 0;
        }

        public static string DescribePathing(Vector2Int? currentNode, Span<MapNode> pathNodes) {
            if (pathNodes.IsEmpty) {
                return "Go to the boss fight";
            }
            var moveToPos = pathNodes[0].position;
            if (currentNode == null) {
                return "Go to the next act";
            }
            var moveFromPos = currentNode.Value;
            var direction = moveFromPos.x > moveToPos.x ? "left" :
                (moveFromPos.x < moveToPos.x ? "right" : "up");
            var destination = pathNodes[0].nodeType.ToString();
            return string.Format("Go {0} to the {1}", direction, destination);
        }

        public static void EvaluatePathing(Evaluation evaluation) {
            var currentNode = PumpernickelSaveState.instance.GetCurrentNode();
            var allPaths = Path.BuildAllPaths(currentNode, evaluation.BonusCardRewards);
            if (!allPaths.Any()) {
                return;
            }
            foreach (var path in allPaths) {
                Scoring.ScorePath(path);
            }
            foreach (var path in allPaths) {
                path.MergeScoreWithOffRamp();
            }
            var bestPath = allPaths.OrderByDescending(x => x.score).First();
            evaluation.MergeScores(bestPath.scores);
            int i = 0;
            while (!evaluation.NeedsMoreInfo && bestPath.planningNodes > i) {
                evaluation.Advice.Add(DescribePathing(currentNode?.position, bestPath.nodes[i..]));
                switch (bestPath.GetNodeType(i)) {
                    case NodeType.Shop:
                    case NodeType.Question:
                    case NodeType.Fight:
                    case NodeType.Elite:
                    case NodeType.MegaElite: {
                        evaluation.NeedsMoreInfo = true;
                        break;
                    }
                    case NodeType.Boss: {
                        evaluation.NeedsMoreInfo = true;
                        evaluation.Advice.Add("Fight " + Save.state.boss);
                        break;
                    }
                    case NodeType.Chest: {
                        evaluation.NeedsMoreInfo = true;
                        evaluation.Advice.Add("Open the chest");
                        break;
                    }
                    case NodeType.Fire: {
                        switch (bestPath.fireChoices[i]) {
                            case FireChoice.Rest: {
                                evaluation.Advice.Add("Rest");
                                break;
                            }
                            case FireChoice.Upgrade: {
                                var bestUpgrade = Evaluators.ChooseBestUpgrade(bestPath, i);
                                evaluation.Advice.Add("Upgrade " + bestUpgrade);
                                break;
                            }
                            case FireChoice.Lift: {
                                evaluation.Advice.Add("Lift");
                                break;
                            }
                            case FireChoice.Key: {
                                evaluation.Advice.Add("Take the red key");
                                break;
                            }
                            default:
                                throw new System.NotImplementedException();
                        }
                        break;
                    }
                }
                currentNode = i < bestPath.nodes.Length ? bestPath.nodes[i] : null;
                i++;
            }
            evaluation.Path = bestPath;
        }

        public static void Evaluate(Evaluation evaluation) {
            EvaluatePathing(evaluation);
            for (int i = 0; i < PumpernickelSaveState.instance.cards.Count; i++) {
                var card = PumpernickelSaveState.instance.cards[i];
                evaluation.AddScore(ScoreReason.DeckQuality, EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(card, i));
            }
            for (int i = 0; i < PumpernickelSaveState.instance.relics.Count; i++) {
                var relicId = PumpernickelSaveState.instance.relics[i];
                // TODO: fix setup relics?
                var relic = Database.instance.relicsDict[relicId];
                evaluation.AddScore(ScoreReason.RelicQuality, EvaluationFunctionReflection.GetRelicEvalFunctionCached(relic.id)(relic));
            }
            Scoring.EvaluateGlobalRules(evaluation);
        }
        public static Evaluation AdviseOnRewards(List<RewardOption> rewardOptions) {
            var currentNode = PumpernickelSaveState.instance.GetCurrentNode();
            var eligibleForBlueKey = currentNode.nodeType == NodeType.Chest && !Save.state.has_sapphire_key;
            var evaluations = MultiplexRewards(rewardOptions, eligibleForBlueKey).ToArray();
            var preRewardEvaluation = new Evaluation();
            Evaluate(preRewardEvaluation);
            Scoring.ApplyVariance(evaluations, preRewardEvaluation);
            return evaluations.OrderByDescending(x => x.Score).First();
        }
    }
}