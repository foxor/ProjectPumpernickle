﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    // There are 2 ways to enforce the limit on the deck size:
    //  1) We want to be able to get an infinite against the heart
    //  2) We want to be able to infinite now if possible
    internal class RewardInfinite : IGlobalRule {
        public static readonly float END_OF_GAME_POINTS = 12f;
        public static readonly float NEMESIS_POINTS = 6f;
        public static readonly float NOW_POINTS = 12f;
        bool IGlobalRule.ShouldApply {
            get {
                return Save.state.buildingInfinite;
            }
        }
        public static float CurrentInfiniteRoom() {
            return Save.state.infiniteMaxSize - Evaluators.PermanentDeckSize() - Save.state.missingCardCount;
        }

        void IGlobalRule.Apply(Evaluation evaluation) {
            var room = CurrentInfiniteRoom();
            var expectedCardRemoves = evaluation.Path.EndOfActPath ? Path.ExpectedFutureActCardRemoves() : evaluation.Path.ExpectedPossibleCardRemoves();
            var expectedPreNemesisRemoves = evaluation.Path.EndOfActPath ? Path.ExpectedFutureActCardRemovesBeforeNemesis() : evaluation.Path.ExpectedPossibleCardRemovesBeforeNemesis();
            var finalRoom = room + expectedCardRemoves;
            var nemesisRoom = room + expectedPreNemesisRemoves;
            var infiniteNow = Lerp.Inverse(-4f, 0f, room);
            var infiniteEnd = Lerp.Inverse(-4f, 0f, finalRoom - 2f);
            var infiniteNemesis = Lerp.Inverse(-4f, 0f, nemesisRoom - 5f);
            var infiniteQuality = Evaluators.CurrentInfiniteQuality();
            if (Save.state.relics.Contains("Medical Kit")) {
                infiniteEnd = Lerp.Inverse(-4f, 0f, finalRoom);
                infiniteNemesis = Lerp.Inverse(-4f, 0f, nemesisRoom);
            }
            evaluation.AddScore(ScoreReason.InfiniteNow, infiniteNow * NOW_POINTS * infiniteQuality);
            evaluation.AddScore(ScoreReason.InfiniteByHeart, infiniteEnd * END_OF_GAME_POINTS * infiniteQuality);
            evaluation.AddScore(ScoreReason.InfiniteNemesis, infiniteNemesis * NEMESIS_POINTS * infiniteQuality);
        }
    }
}
