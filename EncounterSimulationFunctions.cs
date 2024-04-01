using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class EncounterSimulationFunctions {
        public static float ENCCultist(int floorNum) {
            return 1f;
        }
        public static float ENCJawWorm(int floorNum) {
            return 1f;
        }
        public static float ENC2Louse(int floorNum) {
            return 1f;
        }
        public static float ENCSmallSlimes(int floorNum) {
            return 1f;
        }
        public static float ENCGremlinGang(int floorNum) {
            return 1f;
        }
        public static float ENCLargeSlime(int floorNum) {
            return 1f;
        }
        public static float ENCLotsofSlimes(int floorNum) {
            return 1f;
        }
        public static float ENCBlueSlaver(int floorNum) {
            return 1f;
        }
        public static float ENCRedSlaver(int floorNum) {
            return 1f;
        }
        public static float ENC3Louse(int floorNum) {
            return 1f;
        }
        public static float ENC2FungiBeasts(int floorNum) {
            return 1f;
        }
        public static float ENCExordiumThugs(int floorNum) {
            return 1f;
        }
        public static float ENCExordiumWildlife(int floorNum) {
            return 1f;
        }
        public static float ENCLooter(int floorNum) {
            return 1f;
        }
        public static float ENCSphericGuardian(int floorNum) {
            return 1f;
        }
        public static float ENCChosen(int floorNum) {
            return 1f;
        }
        public static float ENCShellParasite(int floorNum) {
            return 1f;
        }
        public static float ENC3Byrds(int floorNum) {
            return 1f;
        }
        public static float ENC2Thieves(int floorNum) {
            return 1f;
        }
        public static float ENCChosenandByrds(int floorNum) {
            return 1f;
        }
        public static float ENCChosenCultist(int floorNum) {
            return 1f;
        }
        public static float ENCSentryandSphere(int floorNum) {
            return 1f;
        }
        public static float ENCSnakePlant(int floorNum) {
            return 1f;
        }
        public static float ENCSnecko(int floorNum) {
            return 1f;
        }
        public static float ENCCenturionandHealer(int floorNum) {
            return 1f;
        }
        public static float ENC3Cultists(int floorNum) {
            return 1f;
        }
        public static float ENCAvocadoRat(int floorNum) {
            return 1f;
        }
        public static float ENC3Darklings(int floorNum) {
            return 1f;
        }
        public static float ENCOrbWalker(int floorNum) {
            return 1f;
        }
        public static float ENC3Shapes(int floorNum) {
            return 1f;
        }
        public static float ENC4Shapes(int floorNum) {
            return 1f;
        }
        public static float ENCMaw(int floorNum) {
            return 1f;
        }
        public static float ENCSphereand2Shapes(int floorNum) {
            return 1f;
        }
        public static float ENCWrithingMass(int floorNum) {
            return 1f;
        }
        public static float ENCTripleJawWurm(int floorNum) {
            return 1f;
        }
        public static float ENCSpireGrowth(int floorNum) {
            return 1f;
        }
        public static float ENCTransient(int floorNum) {
            return 1f;
        }
        public static float ENCGremlinNob(int floorNum) {
            return 1f;
        }
        public static float ENCLagavulin(int floorNum) {
            return 1f;
        }
        public static float ENC3Sentries(int floorNum) {
            return 1f;
        }
        public static float ENCSlavers(int floorNum) {
            return 1f;
        }
        public static float ENCBookofStabbing(int floorNum) {
            return 1f;
        }
        public static float ENCGremlinLeader(int floorNum) {
            return 1f;
        }
        public static float ENCNemesis(int floorNum) {
            return 1f;
        }
        public static float ENCReptomancer(int floorNum) {
            return 1f;
        }
        public static float ENCGiantHead(int floorNum) {
            return 1f;
        }
        public static float ENCShieldandSpear(int floorNum) {
            return 1f;
        }
        public static float ENCHexaghost(int floorNum) {
            return 1f;
        }
        public static float ChanceToSplitGuardian(int floorNum) {
            return FightSimulator.ChanceToHitDamageBreakpoint(49, 2, floorNum);
        }
        public static float ChanceToBlockGuardian16(int floorNum) {
            return FightSimulator.ChanceToHitBlockBreakpoint("The Guardian", 16, 4, floorNum);
        }
        public static float ENCTheGuardian(int floorNum) {
            return 1f - (((4f * ChanceToSplitGuardian(floorNum)) + ChanceToBlockGuardian16(floorNum)) / 5f);
        }
        public static float ENCSlimeBoss(int floorNum) {
            return 1f;
        }
        public static float ENCChamp(int floorNum) {
            return 1f;
        }
        public static float ENCAutomaton(int floorNum) {
            return 1f;
        }
        public static float ENCCollector(int floorNum) {
            return 1f;
        }
        public static float ENCTimeEater(int floorNum) {
            return 1f;
        }
        public static float ENCAwakenedOne(int floorNum) {
            return 1f;
        }
        public static float ENCDonuandDeca(int floorNum) {
            return 1f;
        }
        public static float ENCTheHeart(int floorNum) {
            return 1f;
        }
        public static float ENCMindBloomBossBattle(int floorNum) {
            return 1f;
        }
        public static float ENC2OrbWalkers(int floorNum) {
            return 1f;
        }
        public static float ENCMaskedBandits(int floorNum) {
            return 1f;
        }
        public static float ENCColosseumSlavers(int floorNum) {
            return 1f;
        }
        public static float ENCColosseumNobs(int floorNum) {
            return 1f;
        }
    }
}
