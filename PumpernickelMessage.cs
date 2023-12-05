using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public enum RewardType {
        None,
        Cards,
        Gold,
        Potion,
        Relic,
        Key,
        CardRemove,
        Neow,
    }
    public class PumpernickelMessage {
        protected static StringBuilder stringBuilder = new StringBuilder();
        public static void HandleMessage(string fromJava) {
            stringBuilder.Append(fromJava);
            if (stringBuilder.ToString().EndsWith("Done\n")) {
                var lines = stringBuilder.ToString().Split('\n');
                switch (lines[0]) {
                    case "Reward": {
                        var floor = int.Parse(lines[1]);
                        var didFight = bool.Parse(lines[2]);
                        Program.ParseNewFile(floor, didFight);
                        ParseRewardMessage(lines.Skip(3).Take(lines.Length - 5));
                        break;
                    }
                    case "GreenKey": {
                        var floor = int.Parse(lines[1]);
                        var didFight = false;
                        Program.ParseNewFile(floor, didFight);
                        ParseGreenKeyMessage(lines.Skip(2).Take(lines.Length - 4));
                        break;
                    }
                    case "Event": {
                        var floor = int.Parse(lines[1]);
                        var didFight = false;
                        Program.ParseNewFile(floor, didFight);
                        ParseEventMessage(lines.Skip(2).Take(lines.Length - 4));
                        break;
                    }
                    case "NewDungeon": {
                        Save.state.act_num++;
                        Save.state.room_y = -1;
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
                                var card = Database.instance.cardsDict[cardId];
                                // Do we need a copy constructor so that we're not messing with the database version?
                                card.upgrades = upgrades;
                                Save.state.cards.Add(card);
                            }
                            deckIndex++;
                        }
                        var greenKeyHeaderIndex = lines.FirstIndexOf(x => x.Equals("GreenKey"));
                        for (int i = relicHeaderIndex + 1 + Save.state.relics.Count; i < greenKeyHeaderIndex; i++) {
                            var relicId = lines[i];
                            Save.state.relics.Add(relicId);
                        }
                        PumpernickelAdviceWindow.instance.UpdateAct();
                        ParseGreenKeyMessage(lines[(greenKeyHeaderIndex + 1)..^2]);
                        GivePathingAdvice();
                        break;
                    }
                    case "Shop": {
                        var floor = int.Parse(lines[1]);
                        var didFight = false;
                        Program.ParseNewFile(floor, didFight);
                        ParseShopMessage(lines.Skip(2).Take(lines.Length - 4));
                        break;
                    }
                    case "Neow": {
                        var floor = int.Parse(lines[1]);
                        var didFight = false;
                        Program.ParseNewFile(floor, didFight);
                        ParseNeowMessage(lines.Skip(2).Take(lines.Length - 4));
                        break;
                    }
                    default: {
                        throw new System.NotImplementedException("Unsupported message type: " + lines[0]);
                    }
                }
                stringBuilder.Clear();
            }
        }
        protected static void ParseRewardMessage(IEnumerable<string> rewardGroups) {
            List<RewardOption> rewardOptions = new List<RewardOption>();
            List<string> argumentBuilder = new List<string>();
            RewardType rewardType = RewardType.None;
            foreach (var rewardMember in rewardGroups) {
                if (string.IsNullOrEmpty(rewardMember)) {
                    continue;
                }
                else if (rewardMember == "Cards") {
                    if (rewardType != RewardType.None) {
                        rewardOptions.Add(new RewardOption() { rewardType = rewardType, values = argumentBuilder.ToArray() });
                    }
                    argumentBuilder.Clear();
                    rewardType = RewardType.Cards;
                }
                else if (rewardMember == "Potion") {
                    if (rewardType != RewardType.None) {
                        rewardOptions.Add(new RewardOption() { rewardType = rewardType, values = argumentBuilder.ToArray() });
                    }
                    argumentBuilder.Clear();
                    rewardType = RewardType.Potion;
                }
                else if (rewardMember == "Gold") {
                    if (rewardType != RewardType.None) {
                        rewardOptions.Add(new RewardOption() { rewardType = rewardType, values = argumentBuilder.ToArray() });
                    }
                    argumentBuilder.Clear();
                    rewardType = RewardType.Gold;
                }
                else if (rewardMember == "Relic") {
                    if (rewardType != RewardType.None) {
                        rewardOptions.Add(new RewardOption() { rewardType = rewardType, values = argumentBuilder.ToArray() });
                    }
                    argumentBuilder.Clear();
                    rewardType = RewardType.Relic;
                }
                else if (rewardMember == "Key") {
                    if (rewardType != RewardType.None) {
                        rewardOptions.Add(new RewardOption() { rewardType = rewardType, values = argumentBuilder.ToArray() });
                    }
                    argumentBuilder.Clear();
                    rewardType = RewardType.Key;
                }
                else {
                    argumentBuilder.Add(rewardMember);
                }
            }
            if (rewardType != RewardType.None) {
                rewardOptions.Add(new RewardOption() { rewardType = rewardType, values = argumentBuilder.ToArray() });
            }
            argumentBuilder.Clear();
            PumpernickelAdviceWindow.instance.SetEvaluations(Advice.AdviseOnRewards(rewardOptions));
        }

        protected static void ParseGreenKeyMessage(IEnumerable<string> floorCoordinate) {
            int x = int.Parse(floorCoordinate.First());
            int y = int.Parse(floorCoordinate.Skip(1).Single());
            PumpernickelSaveState.instance.map[Save.state.act_num, x, y].nodeType = NodeType.MegaElite;
        }
        protected static void ParseEventMessage(IEnumerable<string> eventClassName) {
            var javaClassPath = eventClassName.Single();
            var eventName = javaClassPath.Substring(javaClassPath.LastIndexOf('.') + 1);
            var evaluations = (Evaluation[])typeof(EventAdvice).GetMethod(eventName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Invoke(null, null);
            PumpernickelAdviceWindow.instance.SetEvaluations(evaluations);
        }
        protected static void GivePathingAdvice() {
            PumpernickelAdviceWindow.instance.SetEvaluations(Advice.AdviseOnRewards(new List<RewardOption>()));
        }
        protected static void ParseShopMessage(IEnumerable<string> shopOptionLines) {
            RewardType activeRewardType = RewardType.None;
            List<RewardOption> rewardOptions = new List<RewardOption>();
            rewardOptions.Add(new RewardOption() {
                rewardType = RewardType.CardRemove,
                cost = Save.state.purgeCost,
                values = new string[] { "CardRemove" },
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
                        rewardOptions.Add(new RewardOption {
                            cost = price,
                            rewardType = activeRewardType,
                            values = new string[] {
                                id,
                            },
                        });
                        break;
                    }
                }
            }
            PumpernickelAdviceWindow.instance.SetEvaluations(Advice.AdviseOnRewards(rewardOptions));
        }
        protected static void ParseNeowMessage(IEnumerable<string> neowOptionLines) {
            List<string> neowCost = new List<string>();
            List<string> neowRewards = new List<string>();
            foreach (var line in neowOptionLines) {
                var cost = line.Substring(0, line.IndexOf(":"));
                var reward = line.Substring(line.LastIndexOf(" ") + 1);
                neowCost.Add(cost);
                neowRewards.Add(reward);
            }
            List<RewardOption> rewardOptions = new List<RewardOption>() {
                new RewardOption() {
                    neowCost = neowCost.ToArray(),
                    values = neowRewards.ToArray(),
                    rewardType = RewardType.Neow,
                    skippable = false,
                },
            };
            PumpernickelAdviceWindow.instance.SetEvaluations(Advice.AdviseOnRewards(rewardOptions));
        }
    }
}

public static class SplitIEnumerable {
    public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, Func<T, bool> predicate) {
        var r = new List<List<T>>();
        List<T> activeList = null;
        foreach (var item in source) {
            if (predicate(item)) {
                activeList = new List<T>();
                r.Add(activeList);
            }
            else {
                activeList.Add(item);
            }
        }
        return r;
    }
}
