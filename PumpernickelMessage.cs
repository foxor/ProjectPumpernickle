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
                        ParseGreenKeyMessage(lines.Skip(1).Take(lines.Length - 3));
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
                        if (Save.state == null) {
                            Program.ParseLatestFile();
                        }
                        else {
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
                        }
                        PumpernickelAdviceWindow.instance.UpdateAct();
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
            PumpernickelAdviceWindow.instance.SetEvaluation(PathAdvice.AdviseOnRewards(rewardOptions));
        }

        protected static void ParseGreenKeyMessage(IEnumerable<string> floorCoordinate) {
            if (floorCoordinate.Count() == 2) {
                int x = int.Parse(floorCoordinate.First());
                int y = int.Parse(floorCoordinate.Skip(1).Single());
                PumpernickelSaveState.instance.map[Save.state.act_num, x, y].nodeType = NodeType.MegaElite;
            }
        }
        protected static void ParseEventMessage(IEnumerable<string> eventClassName) {
            var javaClassPath = eventClassName.Single();
            var eventName = javaClassPath.Substring(javaClassPath.LastIndexOf('.') + 1);
            var evaluation = typeof(EventAdvice).GetMethod(eventName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Invoke(null, null);
            PumpernickelAdviceWindow.instance.SetEvaluation((Evaluation)evaluation);
        }
        protected static void GivePathingAdvice() {
            PumpernickelAdviceWindow.instance.SetEvaluation(PathAdvice.AdviseOnRewards(new List<RewardOption>()));
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
            PumpernickelAdviceWindow.instance.SetEvaluation(PathAdvice.AdviseOnRewards(rewardOptions));
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
