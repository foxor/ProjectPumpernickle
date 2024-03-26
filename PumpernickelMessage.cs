using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.LinkLabel;

namespace ProjectPumpernickle {
    public enum RewardType {
        None,
        Cards,
        Gold,
        Potion,
        Relic,
        Key,
        CardRemove,
        Event,
        DropPotion,
    }
    public enum EventRewardElement {
        None,
        RANDOM_COLORLESS_2,
        THREE_CARDS,
        ONE_RANDOM_RARE_CARD,
        REMOVE_CARD,
        UPGRADE_CARD,
        RANDOM_COLORLESS,
        TRANSFORM_CARD,
        THREE_SMALL_POTIONS,
        RANDOM_COMMON_RELIC,
        TEN_PERCENT_HP_BONUS,
        HUNDRED_GOLD,
        THREE_ENEMY_KILL,
        REMOVE_TWO,
        TRANSFORM_TWO_CARDS,
        ONE_RARE_RELIC,
        THREE_RARE_CARDS,
        TWO_FIFTY_GOLD,
        TWENTY_PERCENT_HP_BONUS,
        BOSS_RELIC,
        RANDOM_UPGRADE,
        REMOVE_AND_UPGRADE,
        TWO_RANDOM_UPGRADES,
        RELIC_CHANCE,
        CURSED_TOME,
        HEAL,
        MAX_HP,
    }
    public class PumpernickelMessage {
        protected static StringBuilder stringBuilder = new StringBuilder();
        public static void HandleMessages(string fromJava) {
            stringBuilder.Append(fromJava);
            if (stringBuilder.ToString().EndsWith("Done\n")) {
                HandleMessagesInternal();
            }
        }
        protected static void HandleMessagesInternal() {
            var lines = stringBuilder.ToString().Split('\n');
            var messages = lines.Split(x => x.Equals("Done"));
            foreach (var message in messages) {
                var messageLines = message.ToArray();
                if (messageLines.All(x => string.IsNullOrEmpty(x))) {
                    continue;
                }
                HandleMessage(messageLines);
            }
            stringBuilder.Clear();
        }
        public static void HandleMessage(string[] lines) {
            switch (lines[0]) {
                case "Reward": {
                    var floor = int.Parse(lines[1]);
                    var didFight = bool.Parse(lines[2]);
                    Program.ParseNewFile(floor, didFight);
                    ParseRewardMessage(lines.Skip(3));
                    break;
                }
                case "GreenKey": {
                    ParseGreenKeyMessage(lines.Skip(1));
                    break;
                }
                case "Event": {
                    var floor = int.Parse(lines[1]);
                    var didFight = false;
                    Program.ParseNewFile(floor, didFight);
                    ParseEventMessage(lines.Skip(2));
                    break;
                }
                case "NewDungeon": {
                    ParseNewDungeonMessage(lines.Skip(1));
                    break;
                }
                case "Shop": {
                    var floor = int.Parse(lines[1]);
                    var didFight = false;
                    Program.ParseNewFile(floor, didFight);
                    ParseShopMessage(lines.Skip(2));
                    break;
                }
                case "Neow": {
                    var floor = 0;
                    var didFight = false;
                    Program.ParseNewFile(floor, didFight);
                    var seed = long.Parse(lines[1]);
                    Save.state.seed = seed;
                    Save.state.act_num = 1;
                    Save.state.room_x = 0;
                    Save.state.room_y = -1;
                    Program.GenerateMap();
                    ParseNeowMessage(lines.Skip(2));
                    break;
                }
                case "Ooze": {
                    PumpernickelAdviceWindow.instance.AdviceBox.Text = "Click " + --EventAdvice.SCRAP_OOZE_CLICKS_EXPECTED + " more times";
                    break;
                }
                case "Fight": {
                    var floor = int.Parse(lines[1]);
                    var didFight = false;
                    Program.ParseNewFile(floor, didFight);
                    PumpernickelAdviceWindow.instance.AdviceBox.Text = "Your expected health loss: " + FightSimulator.SimulateFight(Database.instance.encounterDict[lines[2]]);
                    break;
                }
                default: {
                    throw new System.NotImplementedException("Unsupported message type: " + lines[0]);
                }
            }
        }
        protected static void ParseRewardMessage(IEnumerable<string> rewardMessage) {
            Dictionary<RewardType, List<string>> rewardGroups = new Dictionary<RewardType, List<string>>();
            List<string> activeGroup = null;
            foreach (var rewardMember in rewardMessage) {
                if (string.IsNullOrEmpty(rewardMember)) {
                    continue;
                }
                else if (rewardMember == "Cards") {
                    rewardGroups[RewardType.Cards] = activeGroup = new List<string>();
                }
                else if (rewardMember == "Potion") {
                    rewardGroups[RewardType.Potion] = activeGroup = new List<string>();
                }
                else if (rewardMember == "Gold") {
                    rewardGroups[RewardType.Gold] = activeGroup = new List<string>();
                }
                else if (rewardMember == "Relic") {
                    rewardGroups[RewardType.Relic] = activeGroup = new List<string>();
                }
                else if (rewardMember == "Key") {
                    rewardGroups[RewardType.Key] = activeGroup = new List<string>();
                }
                else {
                    activeGroup.Add(rewardMember);
                }
            }
            List<RewardOption> rewardOptions = new List<RewardOption>();
            foreach (var group in rewardGroups) {
                if (group.Key == RewardType.Relic) {
                    var values = group.Value.Select(x => EvaluationFunctionReflection.GetRelicOptionSplitFunctionCached(x)()).Merge();
                    var option = RewardOption.Build(values);
                    option.rewardType = RewardType.Relic;
                    rewardOptions.Add(option);
                }
                else {
                    rewardOptions.Add(new RewardOption() {
                        rewardType = group.Key,
                        values = group.Value.ToArray(),
                    });
                }
            }
            Advice.AdviseOnRewards(rewardOptions);
        }

        protected static void ParseGreenKeyMessage(IEnumerable<string> floorCoordinate) {
            int actNum = int.Parse(floorCoordinate.First());
            int x = int.Parse(floorCoordinate.Skip(1).First());
            int y = int.Parse(floorCoordinate.Skip(2).Single());
            if (Save.state == null) {
                Program.lastReportedGreenKeyLocation = new Program.GreenKeyLocation() {
                    actNum = actNum,
                    x = x,
                    y = y
                };
            }
            else {
                Save.state.map[Save.state.act_num, x, y].nodeType = NodeType.MegaElite;
            }
        }
        protected static void ParseEventMessage(IEnumerable<string> eventArguments) {
            var javaClassPath = eventArguments.First();
            var eventName = javaClassPath.Substring(javaClassPath.LastIndexOf('.') + 1);
            var eventFn = typeof(EventAdvice).GetMethod(eventName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var eventFnArguments = new object[] { eventArguments.Skip(1) };
            eventFn.Invoke(null, eventFnArguments);
        }
        protected static void ParseNewDungeonMessage(IEnumerable<string> parameters) {
            var lines = parameters.ToArray();
            var actLine = lines.First();
            if (Save.state == null) {
                PumpernickelSaveState.parsed = new PumpernickelSaveState();
            }
            Save.state.act_num = int.Parse(actLine.Substring(actLine.LastIndexOf(" ") + 1));
            Save.state.current_room = PumpernickelSaveState.NEW_ACT_ROOM;
            Save.state.event_chances = new float[] {
                0f,
                0.1f,
                0.03f,
                0.02f,
            };
            var healthLine = lines.Skip(1).First();
            Save.state.current_health = int.Parse(healthLine.Substring(healthLine.LastIndexOf(" ") + 1));
            var bossLine = lines.Skip(2).First();
            Save.state.boss = bossLine.Substring(bossLine.IndexOf(" ") + 1);
            var relicHeaderIndex = lines.FirstIndexOf(x => x.Equals("Relics: "));
            var deckIndex = 0;
            for (int i = 4; i < relicHeaderIndex; i++) {
                var colonIndex = lines[i].IndexOf(':');
                var cardId = lines[i].Substring(0, colonIndex);
                while (deckIndex < Save.state.cards.Count && !Save.state.cards[deckIndex].id.Equals(cardId)) {
                    Save.state.cards.RemoveAt(deckIndex);
                }
                if (deckIndex >= Save.state.cards.Count) {
                    var upgrades = int.Parse(lines[i].Substring(colonIndex + 1));
                    var cardIndex = Save.state.AddCardById(cardId);
                    var card = Save.state.cards[cardIndex];
                    card.upgrades = upgrades;
                }
                deckIndex++;
            }
            var greenKeyHeaderIndex = lines.FirstIndexOf(x => x.Equals("GreenKey"));
            var relicEndIndex = greenKeyHeaderIndex >= 0 ? greenKeyHeaderIndex : lines.Length - 2;
            for (int i = relicHeaderIndex + 1 + Save.state.relics.Count; i < greenKeyHeaderIndex; i++) {
                var relicId = lines[i];
                Save.state.relics.Add(relicId);
                Save.state.relic_counters.Add(0);
            }
            PumpernickelAdviceWindow.instance.UpdateAct();
            if (greenKeyHeaderIndex >= 0) {
                ParseGreenKeyMessage(lines[(greenKeyHeaderIndex + 1)..(greenKeyHeaderIndex + 3)]);
            }
            Advice.AdviseOnRewards(new List<RewardOption>());
        }
        protected static void ParseShopMessage(IEnumerable<string> shopOptionLines) {
            RewardType activeRewardType = RewardType.None;
            List<RewardOption> rewardOptions = new List<RewardOption>();
            var shopRemoveOptions = Evaluators.ReasonableRemoveTargets();
            rewardOptions.Add(new RewardOption() {
                rewardType = RewardType.CardRemove,
                cost = Save.state.purgeCost,
                values = shopRemoveOptions.Select(x => x.ToString()).ToArray(),
                skippable = true,
            });
            foreach (var line in shopOptionLines) {
                switch (line) {
                    case "Cards": {
                        activeRewardType = RewardType.Cards;
                        break;
                    }
                    case "Relics": {
                        activeRewardType = RewardType.Relic;
                        break;
                    }
                    case "Potions": {
                        activeRewardType = RewardType.Potion;
                        break;
                    }
                    default: {
                        var id = line.Substring(0, line.IndexOf(":"));
                        var price = int.Parse(line.Substring(line.LastIndexOf(" ") + 1));
                        if (activeRewardType == RewardType.Relic) {
                            var values = line.Select(x => EvaluationFunctionReflection.GetRelicOptionSplitFunctionCached(id)()).Merge();
                            var option = RewardOption.Build(values);
                            option.rewardType = RewardType.Relic;
                            option.cost = price;
                            option.skippable = true;
                            rewardOptions.Add(option);
                        }
                        else {
                            rewardOptions.Add(new RewardOption {
                                cost = price,
                                rewardType = activeRewardType,
                                values = new string[] {
                                    id,
                                },
                                skippable = true,
                            });
                        }
                        break;
                    }
                }
            }
            Advice.AdviseOnRewards(rewardOptions);
        }
        protected static void ParseNeowMessage(IEnumerable<string> neowOptionLines) {
            List<string> neowCost = new List<string>();
            List<string> neowRewards = new List<string>();
            foreach (var line in neowOptionLines) {
                var cost = line.Substring(0, line.IndexOf(":"));
                var reward = line.Substring(line.LastIndexOf(" ") + 1);
                if (reward.Equals(EventRewardElement.REMOVE_CARD.ToString())) {
                    foreach (var possibleRemove in Evaluators.ReasonableRemoveTargets()) {
                        neowCost.Add(cost);
                        neowRewards.Add(reward + ": " + possibleRemove);
                    }
                }
                else {
                    neowCost.Add(cost);
                    neowRewards.Add(reward);
                }
            }
            List<RewardOption> rewardOptions = new List<RewardOption>() {
                new RewardOption() {
                    eventCost = neowCost.ToArray(),
                    values = neowRewards.ToArray(),
                    rewardType = RewardType.Event,
                    skippable = false,
                },
            };
            Advice.AdviseOnRewards(rewardOptions);
        }
    }
}

public static class SplitIEnumerable {
    public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, Func<T, bool> predicate) {
        var r = new List<List<T>>();
        List<T> activeList = new List<T>();
        foreach (var item in source) {
            if (predicate(item)) {
                r.Add(activeList);
                activeList = new List<T>();
            }
            else {
                activeList.Add(item);
            }
        }
        if (activeList.Any()) {
            r.Add(activeList);
        }
        return r;
    }
}
