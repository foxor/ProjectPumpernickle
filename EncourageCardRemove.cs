﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class EncourageCardRemove : IGlobalRule {
        public GlobalRuleEvaluationTiming Timing => GlobalRuleEvaluationTiming.PreCardEvaluation;

        public static readonly float NORMAL_DECK_SIZE = 25f;
        public static readonly float LARGE_DECK_SIZE = 40f;
        public static readonly float REWARD_PER_CARD_SMALLER = .15f;

        void IGlobalRule.Apply(Evaluation evaluation) {
            var deckSize = Save.state.cards.Count;
            if (deckSize < NORMAL_DECK_SIZE) {
                evaluation.SetScore(ScoreReason.SmallDeck, REWARD_PER_CARD_SMALLER * (NORMAL_DECK_SIZE - deckSize));
            }
            else {
                var totalPunishment = 0f;
                for(var i = NORMAL_DECK_SIZE; i < Math.Min(deckSize, LARGE_DECK_SIZE); i++) {
                    var punishFactor = Lerp.Inverse(NORMAL_DECK_SIZE, LARGE_DECK_SIZE, i);
                    var punishment = Lerp.From(0f, REWARD_PER_CARD_SMALLER, punishFactor);
                    totalPunishment += punishment;
                }
                evaluation.SetScore(ScoreReason.LargeDeck, totalPunishment);
            }
        }
    }
}
