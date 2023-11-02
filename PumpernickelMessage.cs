using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public enum RewardType {
        None,
        Cards,
        Gold,
        Potions,
        Relic,
        Key,
    }
    public class PumpernickelMessage {
        protected static StringBuilder stringBuilder = new StringBuilder();
        public static void HandleMessage(string fromJava) {
            stringBuilder.Append(fromJava);
            if (stringBuilder.ToString().EndsWith("Done\n")) {
                var lines = stringBuilder.ToString().Split('\n');
                switch (lines[0]) {
                    case "Reward": {
                        ParseCardsMessage(lines.Skip(1).Take(lines.Length - 3));
                        break;
                    }
                    case "GreenKey": {
                        ParseGreenKeyMessage(lines.Skip(1).Take(lines.Length - 3));
                        break;
                    }
                    case "Event": {
                        ParseEventMessage(lines.Skip(1).Take(lines.Length - 3));
                        break;
                    }
                    case "BossRelic": {
                        ParseBossRelicMessage(lines.Skip(1).Take(lines.Length - 3));
                        break;
                    }
                    case "ChangeAct": {
                        Save.state.act_num++;
                        Save.state.room_y = -1;
                        var healthLine = lines.Skip(1).First();
                        Save.state.current_health = int.Parse(healthLine.Substring(healthLine.LastIndexOf(" ") + 1));
                        PumpernickelAdviceWindow.instance.UpdateAct();
                        GivePathingAdvice();
                        break;
                    }
                    case "Shop": {
                        throw new System.NotImplementedException("Unsupported message type: " + lines[0]);
                        break;
                    }
                    default: {
                        throw new System.NotImplementedException("Unsupported message type: " + lines[0]);
                    }
                }
                stringBuilder.Clear();
            }
        }
        protected static void ParseCardsMessage(IEnumerable<string> rewardGroups) {
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
                    rewardType = RewardType.Potions;
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
        protected static void ParseBossRelicMessage(IEnumerable<string> bossRelicOptions) {
            List<RewardOption> rewardOptions = new List<RewardOption>() {
                new RewardOption() {
                    rewardType = RewardType.Relic, values = bossRelicOptions.ToArray()
                },
            };
            PumpernickelAdviceWindow.instance.SetEvaluation(PathAdvice.AdviseOnRewards(rewardOptions));
        }
        protected static void GivePathingAdvice() {
            PumpernickelAdviceWindow.instance.SetEvaluation(PathAdvice.AdviseOnRewards(new List<RewardOption>()));
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
