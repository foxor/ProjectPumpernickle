using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public enum ScoreReason : byte {
        ActSurvival,
        Upgrades,
        RelicCount,
        CardReward,
        Key,
        CurrentEffectiveHealth,
        BringGoldToShop,
        MissingKey,
        DeckQuality,
        RelicQuality,
        AvoidCard,
        AvoidDuplicateCards,
        MissingCards,
        MissingComboPieces,
        SmallDeck,
        LargeDeck,
        Variance,
        BadBottle,
        EarlyMegaElite,
        InfiniteNow,
        InfiniteByHeart,
        InfiniteNemesis,
        EVENT_SUM,
        EVENT_BEGIN,
        OminousForge,
        PleadingVagrant,
        AncientWriting,
        OldBeggar,
        BigFish,
        BonfireSpirits,
        DeadAdventurer,
        Augmenter,
        Duplicator,
        Falling,
        ForgottenAltar,
        TheDivineFountain,
        CouncilofGhosts,
        GoldenIdol,
        GoldenShrine,
        WingStatue,
        KnowingSkull,
        Lab,
        TheSsssserpent,
        LivingWall,
        MaskedBandits,
        MatchandKeep,
        Mushrooms,
        MysteriousSphere,
        Nloth,
        Purifier,
        ScrapOoze,
        SecretPortal,
        SensoryStone,
        ShiningLight,
        TheCleric,
        TheJoust,
        TheLibrary,
        TheMausoleum,
        TheMoaiHead,
        TheWomaninBlue,
        TombofLordRedMask,
        Transmogrifier,
        UpgradeShrine,
        Vampires,
        WheelofChange,
        WindingHalls,
        WorldofGoop,
        MindBloom,
        Nest,
        FaceTrader,
        ANoteForYourself,
        WeMeetAgain,
        Designer,
        TheColosseum,
        EVENT_END,
        COUNT,
    }
    public class Evaluation {
        public static readonly float MAX_ACCEPTABLE_RISK = .8f;
        public static readonly float MIN_ACCEPTABLE_RISK = .05f;

        public static Evaluation Active;
        public float[] Scores = new float[(byte)ScoreReason.COUNT];
        public List<string> Advice = new List<string>();
        public Path Path = null;
        public float RewardVariance;
        public float WorstCaseRewardFactor;
        public int BonusCardRewards;
        public Evaluation OffRamp;
        public bool NeedsMoreInfo = false;

        protected bool hasDescribedPathing;

        public Evaluation(RewardContext context, Path path) {
            Active = this;

            if (context != null) {
                Advice = context.description.ToList();
                RewardVariance = context.chanceOfOutcome;
                WorstCaseRewardFactor = context.worstCaseValueProportion;
                BonusCardRewards = context.bonusCardRewards;
            }
            this.Path = path;
            if (path == null) {
                Path = Path.BuildPath(new MapNode[] { });
            }

            Save.state.earliestInfinite = 0;
            Save.state.buildingInfinite = false;
            Save.state.expectingToRedBlue = Save.state.character == PlayerCharacter.Watcher;
            Save.state.huntingCards.Clear();
        }
        public void MergeScoreWithOffRamp() {
            if (OffRamp == null) {
                return;
            }
            float riskT = Lerp.InverseUncapped(MIN_ACCEPTABLE_RISK, MAX_ACCEPTABLE_RISK, 1f - Path.chanceToSurvive);
            float offRampRiskT = Lerp.InverseUncapped(MIN_ACCEPTABLE_RISK, MAX_ACCEPTABLE_RISK, 1f - OffRamp.Path.chanceToSurvive);
            riskT = (riskT * MathF.E * 2) - MathF.E;
            offRampRiskT = (offRampRiskT * MathF.E * 2) - MathF.E;
            float riskRelevance = PumpernickelMath.Sigmoid(riskT - offRampRiskT);
            for (int i = 0; i < (byte)ScoreReason.COUNT; i++) {
                Scores[i] = Lerp.From(Scores[i], OffRamp.Scores[i], riskRelevance);
            }
        }

        public override string ToString() {
            if (!hasDescribedPathing) {
                DescribePathing();
            }
            return string.Join("\r\n", Advice);
        }

        public void AddScore(ScoreReason reason, float delta) {
            Scores[(byte)reason] += delta;
        }

        public float Score {
            get {
                return Enum.GetValues<ScoreReason>().Where(x => x != ScoreReason.EVENT_SUM && x != ScoreReason.COUNT).Select(x => Scores[(byte)x]).Sum();
            }
        }
        public static string DescribeOffMapPathing(NodeType nodeType) {
            return nodeType switch {
                NodeType.Boss => "Fight " + Save.state.boss,
                NodeType.BossChest => "Open the boss chest",
                NodeType.Fight => "Go to next act",
                _ => throw new NotImplementedException("Node type " + nodeType + " not expected to appear off map"),
            };
        }
        public static string DescribePathing(Vector2Int? currentNode, Span<MapNode> pathNodes) {
            var moveToPos = pathNodes[0].position;
            var moveFromPos = currentNode.Value;
            var direction = moveFromPos.x > moveToPos.x ? "left" :
                (moveFromPos.x < moveToPos.x ? "right" : "up");
            var destination = pathNodes[0].nodeType.ToString();
            return string.Format("Go {0} to the {1}", direction, destination);
        }
        public void DescribePathing() {
            var i = 0;
            var currentNode = PumpernickelSaveState.instance.GetCurrentNode();
            while (!NeedsMoreInfo && Path.remainingFloors > i) {
                var nodeType = Path.nodeTypes[i];
                if (i < Path.nodes.Length) {
                    Advice.Add(DescribePathing(currentNode?.position, Path.nodes[i..]));
                }
                else {
                    Advice.Add(DescribeOffMapPathing(nodeType));
                }
                switch (nodeType) {
                    case NodeType.Shop:
                    case NodeType.Question:
                    case NodeType.Fight:
                    case NodeType.Elite:
                    case NodeType.BossChest:
                    case NodeType.MegaElite: {
                        NeedsMoreInfo = true;
                        break;
                    }
                    case NodeType.Boss: {
                        NeedsMoreInfo = false;
                        break;
                    }
                    case NodeType.Chest: {
                        NeedsMoreInfo = true;
                        Advice.Add("Open the chest");
                        break;
                    }
                    case NodeType.Fire: {
                        switch (Path.fireChoices[i]) {
                            case FireChoice.Rest: {
                                Advice.Add("Rest");
                                break;
                            }
                            case FireChoice.Upgrade: {
                                var bestUpgrade = Evaluators.ChooseBestUpgrade(out var healthPerFloor, Path, i);
                                Advice.Add("Upgrade " + bestUpgrade);
                                break;
                            }
                            case FireChoice.Lift: {
                                Advice.Add("Lift");
                                break;
                            }
                            case FireChoice.Key: {
                                Advice.Add("Take the red key");
                                break;
                            }
                            default:
                            throw new System.NotImplementedException();
                        }
                        break;
                    }
                }
                currentNode = i < Path.nodes.Length ? Path.nodes[i] : null;
                i++;
            }
        }
    }
}
