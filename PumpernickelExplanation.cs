using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

using SysColor = System.Drawing.Color;

namespace ProjectPumpernickle {
    public partial class PumpernickelExplanation : Form {
        public static readonly byte EVENT_BEGIN = (byte)ScoreReason.EVENT_BEGIN;
        public static readonly byte EVENT_NUM = ScoreReason.EVENT_END - ScoreReason.EVENT_BEGIN - 1;
        private int lastMouseOver = 0;
        public struct RichTextBuilder {
            public StringBuilder explanationText;
            public List<int> sectionBegin;
            public List<int> sectionLength;
            public List<FontStyle> sectionFontStyle;
            public List<SysColor> sectionColor;
            public int startIndex;

            public void AddSection(string text, FontStyle style, SysColor color) {
                sectionBegin.Add(explanationText.Length);
                sectionLength.Add(text.Length);
                sectionFontStyle.Add(style);
                sectionColor.Add(color);
                explanationText.Append(text);
            }
        }
        public static readonly string Preamble = "When vegetablebread plays the game, he mostly makes decisions intuitively based on various factors and how important he thinks they are.  Computers can't do that, so all of those factors are converted to numbers and compared to each other.  If you want to read more about the numbers and why, check out the README.\n\n";

        protected List<int> EvaluationLengths = new List<int>();

        public static string[] scoreReasonToStringCache;
        public static string[] ScoreReasonToStringCache {
            get {
                if (scoreReasonToStringCache == null) {
                    scoreReasonToStringCache = Enumerable.Range(0, (byte)ScoreReason.COUNT).Select(x => (ScoreReason)x).Select(reason => {
                        if (reason == ScoreReason.EVENT_SUM) {
                            return "All events";
                        }
                        var regex = new Regex(@"([^ ])([A-Z])");
                        return regex.Replace(reason.ToString(), AddSpace);
                    }).ToArray();

                }
                return scoreReasonToStringCache;
            }
        }

        public static string AddSpace(Match match) {
            return match.Groups[1].Value + " " + match.Groups[2].Value;
        }
        public PumpernickelExplanation() {
            InitializeComponent();
            lastMouseOver = 0;
            explanation.MouseMove += Explanation_MouseMove;
        }

        private void Explanation_MouseMove(object? sender, MouseEventArgs e) {
            var hoveredCharIndex = explanation.GetCharIndexFromPosition(e.Location);
            var evaluationIndex = 0;
            if (EvaluationLengths.Count == 0) {
                return;
            }
            while (hoveredCharIndex > EvaluationLengths[evaluationIndex]) {
                hoveredCharIndex -= EvaluationLengths[evaluationIndex];
                if (evaluationIndex == EvaluationLengths.Count - 1) {
                    break;
                }
                evaluationIndex++;
            }
            if (evaluationIndex != lastMouseOver) {
                lastMouseOver = evaluationIndex;
                PumpernickelAdviceWindow.instance.SetChosenEvaluation(PumpernickelAdviceWindow.instance.Evaluations[evaluationIndex]);
            }
        }

        public static string IndexToComparative(int index) {
            index += 1;
            var mod = index % 10;
            return index switch {
                1 => "Best",
                2 => "Second best",
                3 => "Third best",
                _ => index + "th best"
            };
        }

        public static bool IsEvent(int scoreReason) {
            return scoreReason > EVENT_BEGIN && scoreReason <= EVENT_BEGIN + EVENT_NUM;
        }

        public static void AddScoreExplanation(RichTextBuilder rtb, Evaluation evaluation, float[] winningScore) {
            foreach (var reasonIndex in Enumerable.Range(0, (byte)ScoreReason.COUNT).OrderByDescending(x => evaluation.Scores[x] - winningScore[x])) {
                var isBest = evaluation.Equals(PumpernickelAdviceWindow.instance.Evaluations[0]);
                if (evaluation.Scores[reasonIndex] == 0f) {
                    continue;
                }
                if (IsEvent(reasonIndex)) {
                    continue;
                }
                var score = evaluation.Scores[reasonIndex];
                var delta = score - winningScore[reasonIndex];
                if (delta == 0 && !isBest) {
                    continue;
                }
                var ReasonString = ScoreReasonToStringCache[reasonIndex];
                var deltaText = (delta > 0f ? "+" : "") + delta.ToString("n4");
                rtb.explanationText.Append(ReasonString + ": " + score.ToString("n2"));
                if (!isBest) {
                    rtb.explanationText.Append(" (");
                    rtb.AddSection(deltaText, FontStyle.Regular, delta > 0f ? SysColor.Green : SysColor.Red);
                    rtb.explanationText.Append(") ");
                }
                if (reasonIndex == (byte)ScoreReason.EVENT_SUM) {
                    rtb.AddSection("+", FontStyle.Regular, SysColor.Aqua);
                }
                rtb.explanationText.Append("\n");
            }
        }

        public RichTextBuilder BuildEvaluationString(Evaluation evaluation, int index, float[] winningScore) {
            var rtb = new RichTextBuilder();
            rtb.explanationText = new StringBuilder();
            rtb.sectionBegin = new List<int>();
            rtb.sectionLength = new List<int>();
            rtb.sectionFontStyle = new List<FontStyle>();
            rtb.sectionColor = new List<SysColor>();

            var name = IndexToComparative(index);
            rtb.AddSection(name, FontStyle.Bold, SysColor.Black);
            rtb.explanationText.Append(" Score: " + evaluation.Score.ToString("n2"));
            if (index != 0) {
                var deltaPoints = PumpernickelAdviceWindow.instance.Evaluations[0].Score - evaluation.Score;
                rtb.explanationText.Append(" (");
                rtb.AddSection("-" + deltaPoints.ToString("n4"), FontStyle.Regular, SysColor.Red);
                rtb.explanationText.Append(") ");
            }
            rtb.explanationText.Append("\n");

            var advice = evaluation.ToString().Replace("\r", "");
            rtb.explanationText.Append(advice + "\n\n");

            rtb.AddSection("Score explanation", FontStyle.Italic, SysColor.Black);
            rtb.explanationText.Append(":\n");

            AddScoreExplanation(rtb, evaluation, winningScore);

            rtb.explanationText.Append("\n\n\n");
            return rtb;

        }

        public void Explain(Evaluation[] evaluations) {
            if (evaluations == null) {
                return;
            }
            explanation.Text = Preamble;
            var eventReasons = Enumerable.Range((byte)ScoreReason.EVENT_BEGIN + 1, EVENT_NUM).ToArray();
            foreach (Evaluation evaluation in evaluations) {
                evaluation.Scores[(int)ScoreReason.EVENT_SUM] = eventReasons.Select(x => evaluation.Scores[x]).Sum();
            }
            EvaluationLengths.Clear();
            var explanationBuilders = Enumerable.Range(0, evaluations.Length).Select(x => BuildEvaluationString(evaluations[x], x, evaluations[0].Scores)).ToArray();
            for (var i = 0; i < explanationBuilders.Length; i++) {
                explanationBuilders[i].startIndex = explanation.Text.Length;
                var explanationText = explanationBuilders[i].explanationText;
                var effectiveLength = explanationText.Length;
                if (i == 0) {
                    effectiveLength += explanation.Text.Length;
                }
                explanation.Text += explanationText;
                EvaluationLengths.Add(effectiveLength);
            }
            for (var i = 0; i < explanationBuilders.Length; i++) {
                var explanationStartIndex = explanationBuilders[i].startIndex;
                for (var j = 0; j < explanationBuilders[i].sectionLength.Count; j++) {
                    explanation.SelectionStart = explanationBuilders[i].sectionBegin[j] + explanationStartIndex;
                    explanation.SelectionLength = explanationBuilders[i].sectionLength[j];
                    explanation.SelectionColor = explanationBuilders[i].sectionColor[j];
                    explanation.SelectionFont = new Font(explanation.Font, explanationBuilders[i].sectionFontStyle[j]);
                }
            }
            explanation.Select(0, 0);
        }
    }
}
