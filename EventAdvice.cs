using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class EventAdvice {
        public static Evaluation[] AccursedBlacksmith(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Rummage around in the forge",
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] PleadingVagrant(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] BackToBasics(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] OldBeggar(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] BigFish(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Eat the bananna",
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] BonfireSpirits(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] DeadAdventurer(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] DrugDealer(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Ingest Mutagens"
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] Duplicator(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Duplicate " + Database.instance.cardsDict[Evaluators.BestCopyTarget()].name
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] Falling(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] ForgottenAltar(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] TheDivineFountain(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] CouncilofGhosts(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] GoldenIdolEvent(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Take the golden idol, give up max hp"
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] GoldenShrine(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] GoldenWing(IEnumerable<string> arguments) {
            var preRewardEvaluation = new Evaluation();
            Scoring.Score(preRewardEvaluation);
            var cardRemoveIndex = Evaluators.CardRemoveTarget();
            var existingAdvice = new List<string>(){
                "Pray, removing " + Save.state.cards[cardRemoveIndex].name,
            };
            Save.state.current_health -= 7;
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] KnowingSkull(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] Lab(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] TheSsssserpent(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] LivingWall(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] MaskedBandits(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Fight!",
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] MatchandKeep(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] Mushrooms(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] MysteriousSphere(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] Nloth(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] Purifier(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static int SCRAP_OOZE_CLICKS_EXPECTED = -1;
        public static Evaluation[] ScrapOoze(IEnumerable<string> arguments) {
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
            var evaluations = Advice.AdviseOnRewards(rewardOptions);
            SCRAP_OOZE_CLICKS_EXPECTED = evaluations[0].RewardIndex;
            return evaluations;
        }
        public static Evaluation[] SecretPortal(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] SensoryStone(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] ShiningLight(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] TheCleric(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] TheJoust(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] TheLibrary(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Let's read some books"
            };
            var needsMoreInfo = true;
            return Advice.CreateEventEvaluations(existingAdvice, needsMoreInfo);
        }
        public static Evaluation[] TheMausoleum(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Leave the Mausoleum"
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] TheMoaiHead(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] TheWomaninBlue(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] TombRedMask(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Leave",
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] Transmogrifier(IEnumerable<string> arguments) {
            var advice = Advice.CreateEventEvaluations(new string[0]);
            var remove = Evaluators.CardRemoveTarget();
            // This could lead to remove advice "double coverage" where the above path also prioritizes removing this card
            advice[0].Advice.Insert(0, "Transform the " + Save.state.cards[remove].name);
            return advice;
        }
        public static Evaluation[] UpgradeShrine(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] Vampires(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Refuse the bites"
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] GremlinWheelGame(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] WindingHalls(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Retrace your steps",
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] WorldofGoop(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] MindBloom(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Fight the act 1 boss"
            };
            var needsMoreInfo = true;
            var evals = Advice.CreateEventEvaluations(existingAdvice, needsMoreInfo);
            return evals;
        }
        public static Evaluation[] Nest(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Take the gold"
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] FaceTrader(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] ANoteForYourself(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] WeMeetAgain(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] Designer(IEnumerable<string> arguments) {
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
            return Advice.AdviseOnRewards(rewardOption, existingAdvice);
        }
        public static Evaluation[] Colosseum(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Win the fight",
                "Choose victory",
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] CursedTome(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Read the book"
            };
            return Advice.CreateEventEvaluations(existingAdvice);
        }
    }
}
