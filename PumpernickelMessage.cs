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
                }
                stringBuilder.Clear();
            }
        }
        protected static void AppendAdvice(StringBuilder builder, RewardType rewardType, List<string> argumentBuilder) {
            switch (rewardType) {
                case RewardType.None: {
                    break;
                }
                case RewardType.Cards: {
                    // This should wait until we've seen all the cards rewards before dispatching them
                    builder.Append(PumpernickelBrains.AdviseOnRewards(argumentBuilder));
                    break;
                }
                case RewardType.Relic: {
                    builder.Append("Take the " + argumentBuilder.Single() + "\r\n");
                    break;
                }
                case RewardType.Potions: {
                    builder.Append("Take the potion\r\n");
                    break;
                }
                case RewardType.Gold: {
                    builder.Append("Take the gold\r\n");
                    break;
                }
            }
            argumentBuilder.Clear();
        }
        protected static void ParseCardsMessage(IEnumerable<string> rewardGroups) {
            StringBuilder sb = new StringBuilder();
            List<string> argumentBuilder = new List<string>();
            RewardType rewardType = RewardType.None;
            foreach (var rewardMember in rewardGroups) {
                if (string.IsNullOrEmpty(rewardMember)) {
                    continue;
                }
                else if (rewardMember == "Cards") {
                    AppendAdvice(sb, rewardType, argumentBuilder);
                    rewardType = RewardType.Cards;
                }
                else if (rewardMember == "Potion") {
                    AppendAdvice(sb, rewardType, argumentBuilder);
                    rewardType = RewardType.Potions;
                }
                else if (rewardMember == "Gold") {
                    AppendAdvice(sb, rewardType, argumentBuilder);
                    rewardType = RewardType.Gold;
                }
                else if (rewardMember == "Relic") {
                    AppendAdvice(sb, rewardType, argumentBuilder);
                    rewardType = RewardType.Relic;
                }
                else {
                    argumentBuilder.Add(rewardMember);
                }
            }
            AppendAdvice(sb, rewardType, argumentBuilder);
            PumpernickelAdviceWindow.SetText(sb.ToString());
        }

        protected static void ParseGreenKeyMessage(IEnumerable<string> floorCoordinate) {
            int x = int.Parse(floorCoordinate.First());
            int y = int.Parse(floorCoordinate.Skip(1).Single());
            PumpernickelSaveState.instance.map[x, y].nodeType = NodeType.MegaElite;
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
