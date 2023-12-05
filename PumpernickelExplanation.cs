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
            explanation.MouseMove += Explanation_MouseMove;
        }

        private void Explanation_MouseMove(object? sender, MouseEventArgs e) {
            var hoveredCharIndex = explanation.GetCharIndexFromPosition(e.Location);
            var evaluationIndex = 0;
            while (hoveredCharIndex > EvaluationLengths[evaluationIndex]) {
                hoveredCharIndex -= EvaluationLengths[evaluationIndex];
                evaluationIndex++;
            }
            PumpernickelAdviceWindow.instance.SetChosenEvaluation(PumpernickelAdviceWindow.instance.Evaluations[evaluationIndex]);
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

        public static void AddScoreExplanation(RichTextBuilder rtb, Evaluation evaluation, bool[] irrelevantCategories, float[] categoryAverage) {
            foreach (var reasonIndex in Enumerable.Range(0, (byte)ScoreReason.COUNT).OrderByDescending(x => evaluation.Scores[x] - categoryAverage[x])) {
                if (evaluation.Scores[reasonIndex] == 0f || irrelevantCategories[reasonIndex]) {
                    continue;
                }
                var ReasonString = ScoreReasonToStringCache[reasonIndex];
                var score = evaluation.Scores[reasonIndex];
                var delta = score - categoryAverage[reasonIndex];
                var deltaText = (delta > 0f ? "+" : "") + delta.ToString("n2");
                rtb.explanationText.Append(ReasonString + ": " + score.ToString("n2") + " (");
                rtb.AddSection(deltaText, FontStyle.Regular, delta > 0f ? SysColor.Green : SysColor.Red);
                rtb.explanationText.Append(")\n");
            }
        }

        public RichTextBuilder BuildEvaluationString(Evaluation evaluation, int index, bool[] irrelevantCategories, float[] categoryAverage) {
            var rtb = new RichTextBuilder();
            rtb.explanationText = new StringBuilder();
            rtb.sectionBegin = new List<int>();
            rtb.sectionLength = new List<int>();
            rtb.sectionFontStyle = new List<FontStyle>();
            rtb.sectionColor = new List<SysColor>();

            var name = IndexToComparative(index);
            rtb.AddSection(name, FontStyle.Bold, SysColor.Black);
            rtb.explanationText.Append(" Score: " + evaluation.Score.ToString("n2") + "\n");

            var advice = evaluation.ToString().Replace("\r", "");
            rtb.explanationText.Append(advice + "\n\n");

            rtb.AddSection("Score explanation", FontStyle.Italic, SysColor.Black);
            rtb.explanationText.Append(":\n");

            AddScoreExplanation(rtb, evaluation, irrelevantCategories, categoryAverage);

            rtb.explanationText.Append("\n\n\n");
            return rtb;

        }

        public void Explain(Evaluation[] evaluations) {
            if (evaluations == null) {
                return;
            }
            EvaluationLengths.Clear();
            explanation.Text = "";
            var irrelevantCategories = Enumerable.Range(0, (byte)ScoreReason.COUNT).Select(x => evaluations.Select(e => e.Scores[x]).Distinct().Count() == 1).ToArray();
            var categoryAverages = Enumerable.Range(0, (byte)ScoreReason.COUNT).Select(x => evaluations.Select(e => e.Scores[x]).Average()).ToArray();
            var explanationBuilders = Enumerable.Range(0, evaluations.Length).Select(x => BuildEvaluationString(evaluations[x], x, irrelevantCategories, categoryAverages)).ToArray();
            for (var i = 0; i < explanationBuilders.Length; i++) {
                explanationBuilders[i].startIndex = explanation.Text.Length;
                var explanationText = explanationBuilders[i].explanationText;
                explanation.Text += explanationText;
                EvaluationLengths.Add(explanationText.Length);
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
