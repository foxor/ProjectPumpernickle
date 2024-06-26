﻿namespace ProjectPumpernickle {
    public static class CardFunctions {
        public static float Bash(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Defend_R(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Strike_R(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Anger(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static readonly float ARMAMENTS_VALUE_PER_UNUPGRADED_CARD = .2f;
        public static float Armaments(Card c, int index) {
            var value = 0f;
            var armamentsIndex = Enumerable.Range(0, Save.state.cards.Count).Where(x => Save.state.cards[x].id.Equals("Armaments")).FirstIndexOf(x => x == index);
            var unupgradedCards = Save.state.cards.Where(x => x.upgrades == 0).Count();
            if (armamentsIndex == 0) {
                value += unupgradedCards * ARMAMENTS_VALUE_PER_UNUPGRADED_CARD;
            }
            return value;
        }
        public static float BodySlam(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Clash(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Cleave(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Clothesline(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Flex(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Havoc(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Headbutt(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float HeavyBlade(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float IronWave(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static readonly float PSTRIKE_SCORE_VALUE_PER_STRIKE = 0.2f;
        public static float PerfectedStrike(Card c, int index) {
            var value = Save.state.cards.Where(x => x.name.Contains("Strike")).Count() * PSTRIKE_SCORE_VALUE_PER_STRIKE;
            return value;
        }
        public static float PommelStrike(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ShrugItOff(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SwordBoomerang(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Thunderclap(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float TrueGrit(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float TwinStrike(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Warcry(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float WildStrike(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BattleTrance(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BloodforBlood(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Bloodletting(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BurningPact(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Carnage(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Combust(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DarkEmbrace(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Disarm(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Dropkick(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DualWield(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Entrench(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Evolve(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FeelNoPain(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FireBreathing(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FlameBarrier(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float GhostlyArmor(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Hemokinesis(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float InfernalBlade(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Inflame(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Intimidate(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Metallicize(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float PowerThrough(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Pummel(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Rage(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Rampage(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float RecklessCharge(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Rupture(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SearingBlow(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static readonly float VALUE_PER_THREAT_STATUS = .3f;
        public static float SecondWind(Card c, int index) {
            var value = 0f;
            var targetsInDeck = 2f;
            value += targetsInDeck * VALUE_PER_THREAT_STATUS;
            if (Evaluation.Active.Path != null) {
                foreach (var threat in Evaluation.Active.Path.Threats) {
                    var encounter = Database.instance.encounterDict[threat.Key];
                    var addedStatuses = encounter.expectedStatuses;
                    value += threat.Value * addedStatuses * VALUE_PER_THREAT_STATUS;
                }
            }
            return value;
        }
        public static float SeeingRed(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Sentinel(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SeverSoul(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Shockwave(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SpotWeakness(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Uppercut(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Whirlwind(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Barricade(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Berserk(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Bludgeon(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Brutality(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static readonly float CORRUPTION_OFFSET = -1.25f;
        public static float Corruption(Card c, int index) {
            var first = Save.state.cards.FirstIndexOf(x => x.id.Equals(c.id)) == index;
            if (first) {
                var netCost = Evaluators.AverageCost(c);
                foreach (var card in Save.state.cards) {
                    if (card.cardType == CardType.Skill) {
                        netCost -= Evaluators.AverageCost(card);
                    }
                }
                return -netCost * Scoring.VALUE_PER_NET_SAVING + CORRUPTION_OFFSET;
            }
            else {
                return 0.3f;
            }
        }
        public static float DemonForm(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DoubleTap(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Exhume(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Feed(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FiendFire(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Immolate(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Impervious(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Juggernaut(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float LimitBreak(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Offering(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Reaper(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Defend_G(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Neutralize(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Strike_G(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Survivor(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Acrobatics(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Backflip(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BANE_POISON_VALUE = 0.8f;
        public static float Bane(Card c, int index) {
            var value = 0f;
            var poisonCards = Save.state.cards.Where(x => x.tags.ContainsKey(Tags.Poison.ToString())).Count();
            value += poisonCards / (poisonCards + 1);
            return value;
        }
        public static float BladeDance(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float CloakAndDagger(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DaggerSpray(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DaggerThrow(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DeadlyPoison(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Deflect(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DodgeandRoll(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FlyingKnee(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Outmaneuver(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float PiercingWail(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float PoisonedStab(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Prepared(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float QuickSlash(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Slice(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float UnderhandedStrike(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SuckerPunch(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Accuracy(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float AllOutAttack(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Backstab(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Blur(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BouncingFlask(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float CalculatedGamble(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Caltrops(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static readonly float CATALYST_VALUE_PER_COPIER = 1.5f;
        public static readonly float CATALYST_VALUE_PER_POISON = 0.1f;
        public static float Catalyst(Card c, int index) {
            var value = 0f;
            var copiers = Save.state.cards.Where(x => x.id.Equals("Burst") || x.id.Equals("Night Terror"));
            var poisonTags = Save.state.cards.Select(x => x.tags.TryGetValue(Tags.Poison.ToString(), out var poison) ? poison : 0f);
            value += CATALYST_VALUE_PER_COPIER * copiers.Count();
            value += CATALYST_VALUE_PER_POISON * poisonTags.Sum();
            return value;
        }
        public static float Choke(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static readonly float CONCENTRATE_VALUE_OFFSET = 0.8f;
        public static readonly float CONCENTRATE_EXCESS_COST_VALUE = 2f;
        public static readonly float CONCENTRATE_EXCESS_DRAW_VALUE = 0.5f;
        public static float Concentrate(Card c, int index) {
            var value = 0f;
            var excessPlayableEnergy = Evaluators.ExcessHandCardEnergy();
            var excessEnergyUsefulnessRate = MathF.Max(0f, (excessPlayableEnergy / (excessPlayableEnergy + 2f)) - 0.5f);
            value += excessEnergyUsefulnessRate * CONCENTRATE_EXCESS_COST_VALUE;
            var handRoom = Evaluators.HandRoom();
            var drawPerTurn = Evaluators.SustainableCardDrawPerTurn();
            var overDraw = MathF.Max(0f, drawPerTurn - handRoom);
            value += overDraw * CONCENTRATE_EXCESS_DRAW_VALUE;
            return value;
        }
        public static float CripplingPoison(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Dash(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Distraction(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float EndlessAgony(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float EscapePlan(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static readonly float EVISCERATE_FREE_VALUE = 10f;
        public static readonly float DISCARD_COMBO_BIAS = 1.2f;
        // https://www.wolframalpha.com/input?i=sigmoid%28x+*+1.6%29
        public static readonly float DISCARD_COMBO_SIGMOID_SLOPE = 1.6f;
        public static readonly float EVISCERATE_VALUE_PER_STRENGTH = 0.3f;
        public static float ChanceEviscerateFree() {
            var nonEvisCards = Evaluators.SustainableCardDrawPerTurn() - 1f;
            var avgDiscardTags = Save.state.cards.Select(x => x.setup.GetValueOrDefault(ScoreReason.Discard.ToString())).Average();
            var discards = nonEvisCards * avgDiscardTags;
            var sigmoidX = ((discards - 3) + DISCARD_COMBO_BIAS) * DISCARD_COMBO_SIGMOID_SLOPE;
            var chance = PumpernickelMath.Sigmoid(sigmoidX);
            return chance;
        }
        public static float Eviscerate(Card c, int index) {
            var value = 0f;
            value += ChanceEviscerateFree() * EVISCERATE_FREE_VALUE;
            value += Evaluators.StrengthScaling() * EVISCERATE_VALUE_PER_STRENGTH;
            return value;
        }
        public static float Expertise(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Finisher(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Flechettes(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Footwork(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float HeelHook(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float InfiniteBlades(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float LegSweep(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float MasterfulStab(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float NoxiousFumes(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Predator(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Reflex(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float RiddleWithHoles(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Setup(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Skewer(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Tactician(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Terror(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float WellLaidPlans(Card c, int index) {
            var value = 0f;
            if (Save.state.relics.Contains("Runic Pyramid")) {
                return -10f;
            }
            return value;
        }
        public static float AThousandCuts(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Adrenaline(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float AfterImage(Card c, int index) {
            var value = 0f;
            if (Save.state.buildingInfinite) {
                Save.state.infiniteBlockPerCard += 1f;
            }
            return value;
        }
        public static float Venomology(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BulletTime(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Burst(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float CorpseExplosion(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DieDieDie(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Doppelganger(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Envenom(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float GlassKnife(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float GrandFinale(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Malaise(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float NightTerror(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float PhantasmalKiller(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float StormofSteel(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ToolsoftheTrade(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Unload(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float WraithFormv2(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Defend_B(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Dualcast(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Strike_B(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Zap(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BallLightning(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Barrage(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BeamCell(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ConserveBattery(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Gash(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ColdSnap(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float CompileDriver(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Coolheaded(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float GofortheEyes(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Hologram(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Leap(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Rebound(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Redo(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Stack(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Steam(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Streamline(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SweepingBeam(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Turbo(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Aggregate(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float AutoShields(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Blizzard(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BootSequence(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Lockon(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Capacitor(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Chaos(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Chill(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Consume(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Darkness(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Defragment(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DoomandGloom(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DoubleEnergy(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Undo(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FTL(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ForceField(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Fusion(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float GeneticAlgorithm(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Glacier(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Heatsinks(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float HelloWorld(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Loop(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Melter(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SteamPower(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Recycle(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ReinforcedBody(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Reprogram(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float RipandTear(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Scrape(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SelfRepair(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Skim(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float StaticDischarge(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Storm(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Sunder(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Tempest(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float WhiteNoise(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float AllForOne(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Amplify(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BiasedCognition(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Buffer(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float CoreSurge(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float CreativeAI(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float EchoForm(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Electrodynamics(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Fission(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Hyperbeam(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float MachineLearning(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float MeteorStrike(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float MultiCast(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Rainbow(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Reboot(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Seek(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ThunderStrike(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Defend_P(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Eruption(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Strike_P(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Vigilance(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BowlingBash(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Consecrate(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Crescendo(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float CrushJoints(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float CutThroughFate(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float EmptyBody(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float EmptyFist(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Evaluate(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FlurryOfBlows(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FlyingSleeves(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FollowUp(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Halt(Card c, int index) {
            var value = 0f;
            if (Save.state.expectingToRedBlue) {
                value += 2f;
            }
            else {
                value -= 5f;
            }
            return value;
        }
        public static float JustLucky(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float PathToVictory(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Prostrate(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Protect(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SashWhip(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ThirdEye(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ClearTheMind(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BattleHymn(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float CarveReality(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Collect(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Conclude(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DeceiveReality(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float EmptyMind(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Fasting2(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FearNoEvil(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ForeignInfluence(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Wireheading(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Indignation(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float InnerPeace(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float LikeWater(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Meditate(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float MentalFortress(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Nirvana(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Perseverance(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Pray(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ReachHeaven(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Adaptation(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Sanctity(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SandsOfTime(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SignatureMove(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Vengeance(Card c, int index) {
            // Simmering fury
            var value = 0f;
            return value;
        }
        public static float Study(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Swivel(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float TalkToTheHand(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Tantrum(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Wallop(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float WaveOfTheHand(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Weave(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float WheelKick(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float WindmillStrike(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Worship(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float WreathOfFlame(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Alpha(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Blasphemy(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Brilliance(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ConjureBlade(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DeusExMachina(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DevaForm(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Devotion(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Establishment(Card c, int index) {
            var value = 0f;
            var firstEstablishment = Save.state.cards.FirstIndexOf(x => x.id.Equals("Establishment")) == index;
            if (Save.state.relics.Contains("Snecko Eye")) {
                return 0f;
            }
            if (Save.state.cards.Any(x => x.id.Equals("Meditate")) && firstEstablishment) {
                Save.state.buildingInfinite = true;
                Save.state.expectingToRedBlue = false;
                Save.state.infiniteMaxSize = 11;
                var cardDraw = Evaluators.GetCardDrawCards();
                var drawCount = cardDraw.Count();
                var cardsToAdd = 0;
                if (drawCount < 2) {
                    cardsToAdd += 2 - drawCount;
                }
                var cardDrawCost = Save.state.cards.Where(x => x.tags.ContainsKey(Tags.CardDraw.ToString()) && x.intCost != -1).OrderBy(x => x.intCost).Take(2).Select(x => x.intCost).Sum();
                if (cardsToAdd > 0) {
                    cardDrawCost += cardsToAdd * 1;
                }
                var meditatePlus = Save.state.cards.Any(x => x.id.Equals("Meditate") && x.upgrades > 0);
                var minMeditates = meditatePlus ? 1 : 2;
                var fixedCost = 1 + 1 * minMeditates;
                if (meditatePlus && fixedCost + cardDrawCost <= Evaluators.TurnOneEnergy()) {
                    Save.state.earliestInfinite = 2;
                }
                else {
                    Save.state.earliestInfinite = 3;
                }
                Save.state.missingCardCount = cardsToAdd;
                if (cardsToAdd > 0) {
                    // Should this set some of the Save.state.infiniteBlockPositive type things?
                    Save.state.HuntForCard("EmptyMind");
                    Save.state.HuntForCard("CutThroughFate");
                    Save.state.HuntForCard("Adaptation");
                    Save.state.HuntForCard("InnerPeace");
                    Save.state.HuntForCard("Sanctity");
                    Save.state.HuntForCard("WheelKick");
                    Save.state.HuntForCard("Deep Breath");
                    Save.state.HuntForCard("Flash of Steel");
                    Save.state.HuntForCard("Finesse");
                    Save.state.HuntForCard("Pray");
                }
                else {
                    var drawPositive = cardDraw.Select(x => x.tags[Tags.CardDraw.ToString()] - 1).Sum() > 0;
                    Save.state.infiniteDoesDamage = drawPositive || cardDraw.Any(x => x.tags.ContainsKey(Tags.Damage.ToString()));
                    Save.state.infiniteBlockPerCard = 8f;
                }
            }
            return value;
        }
        public static float Judgement(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float LessonLearned(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float MasterReality(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Omniscience(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Ragnarok(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Scrawl(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SpiritShield(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Vault(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Wish(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Ghostly(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BecomeAlmighty(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Beta(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Bite(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Expunger(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FameAndFortune(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Insight(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float J_A_X_(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float LiveForever(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Miracle(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Omega(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float RitualDagger(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Safety(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Shiv(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Smite(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ThroughViolence(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Burn(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Dazed(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Slimed(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Void(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Wound(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float BandageUp(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Blind(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DarkShackles(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DeepBreath(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Discovery(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float DramaticEntrance(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Enlightenment(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Finesse(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float FlashofSteel(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Forethought(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float GoodInstincts(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Impatience(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float JackOfAllTrades(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Madness(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float MindBlast(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Panacea(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float PanicButton(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Purity(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SwiftStrike(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Trip(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Apotheosis(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Chrysalis(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float HandOfGreed(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Magnetism(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float MasterofStrategy(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Mayhem(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Metamorphosis(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Panache(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SadisticNature(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SecretTechnique(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float SecretWeapon(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float TheBomb(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float ThinkingAhead(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Transmutation(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Violence(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float AscendersBane(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float CurseOfTheBell(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Necronomicurse(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Pride(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Clumsy(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Decay(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Doubt(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Injury(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Normality(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Pain(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Parasite(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Regret(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Shame(Card c, int index) {
            var value = 0f;
            return value;
        }
        public static float Writhe(Card c, int index) {
            var value = 0f;
            return value;
        }
    }
}

public static class MultiplexPairsExtension {
    public static IEnumerable<T[]> MultiplexPairs<T>(this IEnumerable<T> source) {
        var sourceArray = source.ToArray();
        for (int i = 0; i < sourceArray.Length; i++) {
            for (int j = i + 1; j < sourceArray.Length; j++) {
                yield return new T[] { sourceArray[i], sourceArray[j] };
            }
        }
    }
}