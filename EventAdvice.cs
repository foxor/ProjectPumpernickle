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
        public static void Addict(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent(
                new RewardOptionPart() {
                    advice = "Offer gold",
                    value = EventRewardElement.RELIC_CHANCE + ": 1",
                    eventCost = "85",
                    needsMoreInfo = true,
                },
                new RewardOptionPart() {
                    advice = "Rob",
                    value = EventRewardElement.RELIC_CHANCE + ": 1",
                    eventCost = "SHAME",
                    needsMoreInfo = true,
                },
                new RewardOptionPart() {
                    advice = "Leave",
                    value = EventRewardElement.None.ToString(),
                }
            ));
        }
        public static void BackToBasics(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void Beggar(IEnumerable<string> arguments) {
            var validRemoveOptions = Evaluators.ReasonableRemoveTargets();
            var validUpgradeOptions = Evaluators.ReasonableUpgradeTargets();
            var removeOptions = validRemoveOptions.Select(x => new RewardOptionPart() {
                advice = "Offer gold.  Remove: " + Save.state.cards[x].name,
                eventCost = "75",
                value = "REMOVE_CARD: " + x
            });
            var leaveOption = new RewardOptionPart() {
                advice = "Leave",
                value = EventRewardElement.None.ToString(),
            };
            Advice.AdviseOnReward(RewardOption.BuildEvent(removeOptions.Append(leaveOption).ToArray()));
        }
        public static void BigFish(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent(
                new RewardOptionPart() {
                    advice = "Eat the bananna",
                    value = EventRewardElement.HEAL + ": " + Evaluators.PercentHealthHeal(1f / 3f)
                },
                new RewardOptionPart() {
                    advice = "Eat the donut",
                    value = EventRewardElement.MAX_HP + ": 5"
                },
                new RewardOptionPart() {
                    advice = "Open the box",
                    value = EventRewardElement.RELIC_CHANCE + ": 1",
                    eventCost = "REGRET",
                }
            ));
        }
        public static void BonfireSpirits(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void DeadAdventurer(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
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
            var reward = RewardOption.BuildEvent(
                arguments.Select(cardDescriptor =>
                    new RewardOptionPart() {
                        advice = "Lose the " + cardDescriptor,
                        value = EventRewardElement.REMOVE_CARD.ToString() +
                            ": " +
                            Evaluators.IndexOfCardWithDescriptor(cardDescriptor),
                    }
                ).ToArray()
            );
            reward.skippable = false;
            Advice.AdviseOnReward(reward);
        }
        public static void ForgottenAltar(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent(
                /*new RewardOptionPart() {
                    advice = "Get bloody idol",
                },*/
                new RewardOptionPart() {
                    advice = "Sacrifice",
                    value = EventRewardElement.MAX_HP + ": 5",
                    hpCost = Evaluators.PercentHealthDamage(.35f),
                },
                new RewardOptionPart() {
                    advice = "Desecrate",
                    value = EventRewardElement.None.ToString(),
                    eventCost = "DECAY",
                }
            ));
        }
        public static void TheDivineFountain(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void CouncilofGhosts(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void GoldenIdolEvent(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Take the golden idol, give up max hp"
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void GoldenShrine(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void GoldenWing(IEnumerable<string> arguments) {
            /*var preRewardEvaluation = new Evaluation();
            Scoring.ScoreBasedOnEvaluation(preRewardEvaluation);
            var cardRemoveIndex = Evaluators.CardRemoveTarget();
            var existingAdvice = new List<string>(){
                "Pray, removing " + Save.state.cards[cardRemoveIndex].name,
            };
            Save.state.current_health -= 7;
            Advice.AdviseOnRewards(null, existingAdvice);*/
        }
        public static void KnowingSkull(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void Lab(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void TheSsssserpent(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void LivingWall(IEnumerable<string> arguments) {
            var validRemoveOptions = Evaluators.ReasonableRemoveTargets();
            var validUpgradeOptions = Evaluators.ReasonableUpgradeTargets();
            var removeOptions = validRemoveOptions.Select(x => new RewardOptionPart() {
                advice = "Remove " + Save.state.cards[x].name,
                eventCost = "NONE",
                value = "REMOVE_CARD: " + x
            });
            var transformOptions = validRemoveOptions.Select(x => new RewardOptionPart() {
                advice = "Transform " + Save.state.cards[x].name,
                eventCost = "NONE",
                value = "TRANSFORM_CARD: " + x
            });
            var upgradeOptions = validUpgradeOptions.Select(x => new RewardOptionPart() {
                advice = "Upgrade " + Save.state.cards[x].name,
                eventCost = "NONE",
                value = "UPGRADE_CARD: " + x
            });
            var rewardOption = RewardOption.Build(removeOptions.Concat(transformOptions).Concat(upgradeOptions));
            rewardOption.skippable = false;
            rewardOption.rewardType = RewardType.Event;
            Advice.AdviseOnReward(rewardOption);
        }
        public static void MaskedBandits(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Fight!",
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void MatchandKeep(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void Mushrooms(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void MysteriousSphere(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void Nloth(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void Purifier(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
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
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void SensoryStone(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void ShiningLight(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent(
                new RewardOptionPart() {
                    advice = "Enter",
                    hpCost = Evaluators.PercentHealthDamage(.3f),
                    value = EventRewardElement.TWO_RANDOM_UPGRADES.ToString(),
                    needsMoreInfo = true,
                },
                new RewardOptionPart() {
                    advice = "Leave",
                }
            ));
        }
        public static void Cleric(IEnumerable<string> arguments) {
            var removeParts = Evaluators.ReasonableRemoveTargets().Select(x =>
                new RewardOptionPart() {
                    advice = "Remove " + Save.state.cards[x].name,
                    value = EventRewardElement.REMOVE_CARD + ": " + x,
                    eventCost = "75",
                }
            );
            var alwaysParts = new RewardOptionPart[] {
                new RewardOptionPart() {
                    advice = "Heal",
                    value = EventRewardElement.HEAL + ": " + Evaluators.PercentHealthHeal(.25f),
                    eventCost = "35",
                },
                new RewardOptionPart() {
                    advice = "Leave",
                    eventCost = "NONE",
                    value = EventRewardElement.None.ToString(),
                }
            };
            var rewardOption = RewardOption.Build(removeParts.Concat(alwaysParts));
            rewardOption.rewardType = RewardType.Event;
            rewardOption.skippable = false;
            Advice.AdviseOnReward(rewardOption);
        }
        public static void TheJoust(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
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
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void TheWomaninBlue(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
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
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void Vampires(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>() {
                "Refuse the bites"
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void GremlinWheelGame(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void WindingHalls(IEnumerable<string> arguments) {
            var existingAdvice = new List<string>(){
                "Retrace your steps",
            };
            Advice.AdviseOnRewards(null, existingAdvice);
        }
        public static void WorldofGoop(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
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
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void ANoteForYourself(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
        }
        public static void WeMeetAgain(IEnumerable<string> arguments) {
            Advice.AdviseOnReward(RewardOption.BuildEvent());
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
            var option = new RewardOption() {
                advice = new string[] { "Read the book", "Leave" },
                hpCost = new int[] { 21, 0 },
                rewardType = RewardType.Event,
                values = new string[] { EventRewardElement.CURSED_TOME.ToString(), EventRewardElement.None.ToString() },
                skippable = false,
            };
            Advice.AdviseOnReward(option);
        }
    }
}
