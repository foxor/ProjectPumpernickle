using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class EventAdvice {
        public static void AccursedBlacksmith(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Rummage around in the forge",
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void PleadingVagrant(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void BackToBasics(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void OldBeggar(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void BigFish(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Eat the bananna",
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void BonfireSpirits(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void DeadAdventurer(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void DrugDealer(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Ingest Mutagens"
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void Duplicator(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Duplicate " + Database.instance.cardsDict[Evaluators.BestCopyTarget()].name
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void Falling(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void ForgottenAltar(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void TheDivineFountain(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void CouncilofGhosts(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void GoldenIdolEvent(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Take the golden idol, give up max hp"
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void GoldenShrine(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void GoldenWing(IEnumerable<string> arguments) {
            var preRewardEvaluation = new Evaluation();
            Scoring.Score(preRewardEvaluation);
            var cardRemoveIndex = Evaluators.CardRemoveTarget();
            var existingAdvice = new List<string>(){
                "Pray, removing " + Save.state.cards[cardRemoveIndex].name,
            };
            Save.state.current_health -= 7;
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void KnowingSkull(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void Lab(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void TheSsssserpent(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void LivingWall(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void MaskedBandits(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Fight!",
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void MatchandKeep(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void Mushrooms(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void MysteriousSphere(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void Nloth(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void Purifier(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static int SCRAP_OOZE_CLICKS_EXPECTED = -1;
        public static void ScrapOoze(IEnumerable<string> arguments) {
            var adviceList = new List<string>();
            var hpCostList = new List<int>();
            var relicChanceList = new List<float>();
            var numClicks = 0;
            var marginalCost = 5;
            var marginalChance = 0f;
            var totalCost = 0;
            var totalChance = 0f;
            while (totalCost < Save.state.current_health) {
                if (numClicks == 0) {
                    adviceList.Add("Leave");
                }
                else {
                    adviceList.Add("Click up to " + numClicks + " times for " + totalCost + " health");
                }
                hpCostList.Add(totalCost);
                relicChanceList.Add(totalChance);

                totalCost += marginalCost;
                marginalCost += 1;
                marginalChance += numClicks == 0 ? .25f : .1f;
                totalChance = (1 - totalChance) * marginalChance + totalChance;
                numClicks++;
            }
            var option = new RewardOption() {
                advice = adviceList.ToArray(),
                hpCost = hpCostList.ToArray(),
                rewardType = RewardType.Event,
                eventCost = hpCostList.Select(x => "NONE").ToArray(),
                skippable = false,
                values = relicChanceList.Select(x => "RELIC_CHANCE: " + x).ToArray(),
            };
            var rewardOptions = new List<RewardOption>() {
                option
            };
            Advice.AdviseOnRewards(rewardOptions);
            SCRAP_OOZE_CLICKS_EXPECTED = (int)PumpernickelAdviceWindow.instance.Evaluations[0].RewardIndex;
        }
        public static void SecretPortal(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void SensoryStone(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void ShiningLight(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void TheCleric(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void TheJoust(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void TheLibrary(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Let's read some books"
            };
            var needsMoreInfo = true;
            Advice.AdviseOnRewards(null, existingAdvice, needsMoreInfo);
        }
        public static void TheMausoleum(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Leave the Mausoleum"
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void TheMoaiHead(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void TheWomaninBlue(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void TombRedMask(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Leave",
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void Transmogrifier(IEnumerable<string> arguments) {
            var targets = Evaluators.ReasonableRemoveTargets();
            var rewardOption = new RewardOption() {
                rewardType = RewardType.Event,
                advice = targets.Select(x => "Transform the " + Save.state.cards[x].name).ToArray(),
                values = targets.Select(x => EventRewardElement.TRANSFORM_CARD.ToString() + ": " + x).ToArray(),
                skippable = true,
            };
            Advice.AdviseOnRewards(new List<RewardOption>() { rewardOption }, needsMoreInfo: true);
        }
        public static void UpgradeShrine(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void Vampires(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Refuse the bites"
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void GremlinWheelGame(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void WindingHalls(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Retrace your steps",
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void WorldofGoop(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void MindBloom(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Fight the act 1 boss"
            };
            var needsMoreInfo = true;
            Advice.AdviseOnRewards(null, existingAdvice, needsMoreInfo);
        }
        public static void Nest(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Take the gold"
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void FaceTrader(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void ANoteForYourself(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void WeMeetAgain(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void Designer(IEnumerable<string> arguments) {
            var adjustmentUpgradesOne = bool.Parse(arguments.First());
            var cleanUpRemovesCards = bool.Parse(arguments.Skip(1).First());
            var existingAdvice = new List<string>() {
                "Talk to the designer"
            };
            var adjustmentElement = adjustmentUpgradesOne ? EventRewardElement.UPGRADE_CARD : EventRewardElement.TWO_RANDOM_UPGRADES;
            var cleanupElement = cleanUpRemovesCards ? EventRewardElement.REMOVE_CARD : EventRewardElement.TRANSFORM_TWO_CARDS;
            var rewardOption = new List<RewardOption>() {
                new RewardOption() {
                    eventCost = new string[] { "50", "75", "110", "0" },
                    rewardType = RewardType.Event,
                    values = new string[] {
                        adjustmentElement.ToString(),
                        cleanupElement.ToString(),
                        EventRewardElement.REMOVE_AND_UPGRADE.ToString(),
                        EventRewardElement.None.ToString(),
                    },
                    hpCost = new int[] {
                        0,
                        0,
                        0,
                        5,
                    },
                    advice = new string[] {
                        "Take the adjustments",
                        "Take the clean up",
                        "Take the full service",
                        "Punch him!",
                    },
                    skippable = false,
                }
            };
            Advice.AdviseOnRewards(rewardOption, existingAdvice);
        }
        public static void Colosseum(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Win the fight",
                "Choose victory",
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void CursedTome(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Read the book"
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
    }
}
