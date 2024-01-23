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
        AccursedBlacksmith,
        PleadingVagrant,
        BackToBasics,
        OldBeggar,
        BigFish,
        BonfireSpirits,
        DeadAdventurer,
        DrugDealer,
        Duplicator,
        Falling,
        ForgottenAltar,
        TheDivineFountain,
        CouncilofGhosts,
        GoldenIdol,
        GoldenShrine,
        GoldenWing,
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
        TombRedMask,
        Transmogrifier,
        UpgradeShrine,
        Vampires,
        GremlinWheelGame,
        WindingHalls,
        WorldofGoop,
        MindBloom,
        Nest,
        FaceTrader,
        ANoteForYourself,
        WeMeetAgain,
        Designer,
        Colosseum,
        CursedTome,
        EVENT_END,
        SkillIntoNob,
        PickedNeededCard,
        WingedBootsCharges,
        WingedBootsFlexibility,
        COUNT,
    }
    public class Evaluation {
        public static readonly float MAX_ACCEPTABLE_RISK = .8f;
        public static readonly float MIN_ACCEPTABLE_RISK = .05f;

        [ThreadStatic]
        public static Evaluation Active;
        public float[] Scores = new float[(byte)ScoreReason.COUNT];
        public List<string> Advice = new List<string>();
        public Path Path = null;
        public float Likelihood = 1f;
        public float WorstCaseRewardFactor;
        public float AverageCaseRewardFactor;
        public int BonusCardRewards;
        public Evaluation OffRamp;
        public bool NeedsMoreInfo = false;
        public int RewardIndex;
        public int Id;

        protected bool hasDescribedPathing;

        public Evaluation(RewardContext context = null, int Id = -1, int RewardIndex = -1) {
            Active = this;
            this.Id = Id;
            this.RewardIndex = RewardIndex;

            if (context != null) {
                Advice = context.description.ToList();
                Likelihood = context.chanceOfOutcome;
                WorstCaseRewardFactor = context.worstCaseValueProportion;
                AverageCaseRewardFactor = context.averageCaseValueProportion;
                BonusCardRewards = context.bonusCardRewards;
            }

            Save.state.earliestInfinite = 0;
            Save.state.expectingToRedBlue = Save.state.character == PlayerCharacter.Watcher;
            Save.state.buildingInfinite = Save.state.expectingToRedBlue;
            Save.state.huntingCards.Clear();
        }
        public void SetPath(Path path, int startingCardRewards) {
            Path = path;
            Path.ExplorePath(startingCardRewards);
        }
        public void MergeScoreWithOffRamp() {
            if (OffRamp == null) {
                return;
            }
            float riskT = Lerp.InverseUncapped(MIN_ACCEPTABLE_RISK, MAX_ACCEPTABLE_RISK, 1f - Path.chanceToWin);
            float offRampRiskT = Lerp.InverseUncapped(MIN_ACCEPTABLE_RISK, MAX_ACCEPTABLE_RISK, 1f - OffRamp.Path.chanceToWin);
            var dT = riskT - offRampRiskT;
            var sigmoidX = -5f + dT * 10f;
            float riskRelevance = PumpernickelMath.Sigmoid(sigmoidX);
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
                NodeType.Animation => "Open the door to act 4",
                NodeType.Fire => "Go to act 4",
                NodeType.Shop => "Go to the shop",
                _ => throw new NotImplementedException("Node type " + nodeType + " not expected to appear off map"),
            };
        }
        public static string DescribePathing(Vector2Int? currentNode, Span<MapNode> pathNodes) {
            var moveToPos = pathNodes[0].position;
            var moveFromPos = currentNode.Value;
            var direction = moveFromPos.x > moveToPos.x ? "left" :
                (moveFromPos.x < moveToPos.x ? "right" : "up");
            if (moveFromPos.y == Evaluators.ActToFirstFloor(Save.state.act_num) - 1) {
                direction = "as marked";
            }
            var destination = pathNodes[0].nodeType.ToString();
            return string.Format("Go {0} to the {1}", direction, destination);
        }
        public void DescribePathing() {
            var i = 0;
            var currentNode = Save.state.GetCurrentNode();
            // Something about this caused off-map pathing when neow gave a random rare card?
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
                            default: {
                                throw new System.NotImplementedException();
                            }
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
