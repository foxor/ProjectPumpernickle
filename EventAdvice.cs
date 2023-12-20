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
        public static Evaluation[] AncientWriting(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] OldBeggar(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
        public static Evaluation[] BigFish(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
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
        public static Evaluation[] Augmenter(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
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
        public static Evaluation[] WingStatue(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
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
            var existingAdvice = new List<string>(){};
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
        public static Evaluation[] ScrapOoze(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
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
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
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
        public static Evaluation[] WheelofChange(IEnumerable<string> arguments) {
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
        public static Evaluation[] TheColosseum(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){};
            return Advice.CreateEventEvaluations(existingAdvice);
        }
    }
}
