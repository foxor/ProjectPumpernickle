using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ProjectPumpernickle {
    public partial class PumpernickelAdviceWindow : Form {
        public static PumpernickelAdviceWindow instance = null;
        protected int lastHoveredIndex = -1;
        protected string[] PathTexts = new string[0];
        protected System.Drawing.Color defaultColor;
        public Evaluation[] Evaluations = null;
        protected PumpernickelExplanation explainForm;
        protected Evaluation ChosenEvaluation;

        public PumpernickelAdviceWindow() {
            InitializeComponent();
            instance = this;
            whyButton.Click += WhyButton_Click;
        }

        private void WhyButton_Click(object? sender, EventArgs e) {
            explainForm = new PumpernickelExplanation();
            explainForm.Explain(Evaluations);
            explainForm.Show();
        }

        private void LoadForm(object sender, EventArgs e) {
            PathPreview.MouseMove += OnPathMouseMove;
            PathPreview.MouseLeave += OnPathMouseLeave;
            defaultColor = PathPreview.ForeColor;
            Program.OnStartup();
        }

        public static void SetPathTexts(string[] paths) {
            instance.PathTexts = paths;
            instance.UpdateAct();
        }

        public void UpdateAct() {
            if (Save.state.act_num == 4) {
                return;
            }
            PathPreview.Text = PathTexts[Save.state.act_num - 1];
        }

        public static Vector2Int IndexToPosition(int index, out bool IsValid) {
            IsValid = true;
            var charX = index % 21;
            var charY = index / 21;
            if ((charX % 3) != 0) {
                IsValid = false;
            }
            if (charY % 2 != 0) {
                IsValid = false;
            }
            return new Vector2Int(charX / 3, 14 - (charY / 2));
        }

        public static int PositionToIndex(Vector2Int position) {
            var charX = position.x * 3;
            var charY = (14 - position.y) * 2;
            return charX + charY * 21;
        }

        private void OnPathMouseMove(object? sender, MouseEventArgs e) {
            if (Evaluations == null) {
                return;
            }
            var hoveredCharIndex = PathPreview.GetCharIndexFromPosition(e.Location);
            if (hoveredCharIndex != lastHoveredIndex) {
                var asCoord = IndexToPosition(hoveredCharIndex, out var isValid);
                isValid &= PathPreview.Text[hoveredCharIndex] != ' ';
                if (isValid && ChosenEvaluation?.Path != null) {
                    lastHoveredIndex = hoveredCharIndex;
                    var pathIndex = ChosenEvaluation.Path.nodes.FirstIndexOf(x => x.position.Equals(asCoord));
                    if (pathIndex != -1) {
                        PathNodeInfoBox.Text =
                            "Expected Health: " + ChosenEvaluation.Path.expectedHealth[pathIndex] + "\n" +
                            "Worst Case Health: " + ChosenEvaluation.Path.worstCaseHealth[pathIndex] + "\n" +
                            "Expected Gold: " + ChosenEvaluation.Path.expectedGold[pathIndex] + "\n" +
                            "Defensive Power: " + ChosenEvaluation.Path.projectedDefensivePower[pathIndex] + "\n"
                        ;
                        var chosenNode = ChosenEvaluation.Path.nodes[pathIndex];
                        if (chosenNode.nodeType == NodeType.Shop) {
                            PathNodeInfoBox.Text +=
                                "Shop Plan: " + ChosenEvaluation.Path.shortTermShopPlan.ToString();
                        }
                        if (chosenNode.nodeType == NodeType.Fire) {
                            PathNodeInfoBox.Text +=
                                "Fire Choice: " + ChosenEvaluation.Path.fireChoices[pathIndex].ToString();
                        }
                    }
                }
                else {
                    PathNodeInfoBox.Text = string.Empty;
                    lastHoveredIndex = -1;
                }
            }
        }
        private void OnPathMouseLeave(object? sender, EventArgs e) {
            PathNodeInfoBox.Text = string.Empty;
            lastHoveredIndex = -1;
        }

        public void SetEvaluations(Evaluation[] evaluations) {
            Evaluations = evaluations;
            SetChosenEvaluation(evaluations.First());
            if (explainForm != null && !explainForm.IsDisposed) {
                explainForm.Explain(Evaluations);
            }
        }

        public void SetChosenEvaluation(Evaluation chosenEvaluation) {
            ChosenEvaluation = chosenEvaluation;
            instance.AdviceBox.Text = chosenEvaluation.ToString();
            UpdateAct();
            if (chosenEvaluation.Path != null) {
                foreach (var pathNode in chosenEvaluation.Path.nodes) {
                    var charIndex = PositionToIndex(pathNode.position);
                    PathPreview.SelectionStart = charIndex;
                    PathPreview.SelectionLength = 1;
                    PathPreview.SelectionColor = System.Drawing.Color.Red;
                }
            }
        }

        public void DisplayException(Exception exception) {
            AdviceBox.Text = exception.ToString();
        }
    }
}