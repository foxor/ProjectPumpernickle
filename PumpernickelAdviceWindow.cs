using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;

namespace ProjectPumpernickle {
    public partial class PumpernickelAdviceWindow : Form {
        public static PumpernickelAdviceWindow instance = null;
        protected int lastHoveredIndex = -1;
        protected Vector2Int lastHoveredCoord;
        protected string[] PathTexts = new string[0];
        protected System.Drawing.Color defaultColor;
        public Evaluation[] Evaluations = null;
        public Evaluation[] FilteredEvaluations = null;
        public long ChunksComplete;
        public long TotalChunks;
        protected PumpernickelExplanation explainForm;
        protected Evaluation ChosenEvaluation;
        protected List<Vector2Int> RequiredCoords = new List<Vector2Int>();
        protected int lastAct = -1;

        public PumpernickelAdviceWindow() {
            InitializeComponent();
            instance = this;
            whyButton.MouseDown += new MouseEventHandler(WhyButton_Click);
            PathPreview.KeyPress += PathPreview_KeyPress;
        }
        private void LoadForm(object sender, EventArgs e) {
            PathPreview.MouseMove += OnPathMouseMove;
            PathPreview.MouseLeave += OnPathMouseLeave;
            defaultColor = PathPreview.ForeColor;
            Program.OnStartup();
        }
        public bool EvalFitsCoords(Evaluation eval) {
            foreach (var coord in RequiredCoords) {
                if (!eval.Path.nodes.Any(x => x.position.Equals(coord))) {
                    return false;
                }
            }
            return true;
        }
        public bool PickBestEvalThroughCoords() {
            var chosen = Evaluations.Where(EvalFitsCoords);
            if (!chosen.Any()) {
                chosen = Advice.RequestUnprunedMerge().Where(EvalFitsCoords);
                if (!chosen.Any()) {
                    return false;
                }
            }
            SetFiltererdEvaluations(chosen.ToArray());
            return true;
        }

        private void WhyButton_Click(object? sender, EventArgs e) {
            explainForm = new PumpernickelExplanation();
            explainForm.Explain(ExplanationGroupMode.Reward);
            explainForm.Show();
        }

        public static void SetPathTexts(string[] paths) {
            instance.PathTexts = paths;
            instance.UpdateAct();
        }

        public void UpdateAct() {
            if (Save.state.act_num == 4 || Save.state.act_num == lastAct) {
                return;
            }
            lastAct = Save.state.act_num;
            RequiredCoords.Clear();
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
                lastHoveredCoord = IndexToPosition(hoveredCharIndex, out var isValid);
                isValid &= PathPreview.Text[hoveredCharIndex] != ' ';
                if (isValid && ChosenEvaluation?.Path != null) {
                    lastHoveredIndex = hoveredCharIndex;
                    var pathIndex = ChosenEvaluation.Path.nodes.FirstIndexOf(x => x.position.Equals(lastHoveredCoord));
                    if (pathIndex != -1) {
                        PathNodeInfoBox.Text =
                            "Expected health: " + ChosenEvaluation.Path.expectedHealth[pathIndex] + "\n" +
                            "Worst case health: " + ChosenEvaluation.Path.worstCaseHealth[pathIndex] + "\n" +
                            "Expected gold: " + ChosenEvaluation.Path.expectedGold[pathIndex] + "\n" +
                            "Defensive power: " + ChosenEvaluation.Path.projectedDefensivePower[pathIndex] + "\n" +
                            "Chance of death: " + ChosenEvaluation.Path.chanceOfDeath[pathIndex] + "\n" +
                            "Expected upgrades: " + ChosenEvaluation.Path.expectedUpgrades[pathIndex] + "\n" +
                            "Expected card rewards: " + ChosenEvaluation.Path.expectedCardRewards[pathIndex] + "\n"
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
                    else {
                        PathNodeInfoBox.Text = "Press 'f' to filter for evaluations that pass through this node";
                    }
                }
                else {
                    PathNodeInfoBox.Text = string.Empty;
                    lastHoveredIndex = -1;
                }
            }
        }
        private void PathPreview_KeyPress(object? sender, KeyPressEventArgs e) {
            if (e.KeyChar == 'f') {
                RequiredCoords.Add(lastHoveredCoord);
                var anyFit = PickBestEvalThroughCoords();
                if (!anyFit) {
                    RequiredCoords.Remove(lastHoveredCoord);
                    PickBestEvalThroughCoords();
                }
            }
        }
        private void OnPathMouseLeave(object? sender, EventArgs e) {
            PathNodeInfoBox.Text = string.Empty;
            lastHoveredIndex = -1;
        }

        public void SetEvaluations(Evaluation[] evaluations, long chunksComplete, long totalChunks) {
            TotalChunks = totalChunks;
            ChunksComplete = chunksComplete;
            if (chunksComplete < totalChunks && PumpernickelExplanation.BlockPartialUpdates) {
                return;
            }
            PumpernickelExplanation.BlockPartialUpdates = false;
            Evaluations = evaluations;
            if (RequiredCoords.Count > 0) {
                PickBestEvalThroughCoords();
            }
            else {
                SetFiltererdEvaluations(evaluations);
            }
        }

        public void SetFiltererdEvaluations(Evaluation[] evaluations) {
            FilteredEvaluations = evaluations;
            SetChosenEvaluation(evaluations.First());
        }

        public void SetChosenEvaluation(Evaluation chosenEvaluation) {
            var adviceText = new StringBuilder();
            if (TotalChunks > 1 && ChunksComplete < TotalChunks) {
                adviceText.AppendLine(String.Format("Still thinking, {0:P2} complete", (ChunksComplete * 1f / TotalChunks)));
            }
            adviceText.Append(chosenEvaluation.ToString());
            ChosenEvaluation = chosenEvaluation;
            instance.AdviceBox.Text = adviceText.ToString();
            UpdateAct();
            PathPreview.Text = PathTexts[Save.state.act_num - 1];
            if (chosenEvaluation.OffRamp != null) {
                foreach (var pathNode in chosenEvaluation.OffRamp.Path.nodes) {
                    var charIndex = PositionToIndex(pathNode.position);
                    if (charIndex < 0) {
                        continue;
                    }
                    PathPreview.SelectionStart = charIndex;
                    PathPreview.SelectionLength = 1;
                    PathPreview.SelectionColor = System.Drawing.Color.Blue;
                }
            }
            if (chosenEvaluation.Path != null) {
                foreach (var pathNode in chosenEvaluation.Path.nodes) {
                    var charIndex = PositionToIndex(pathNode.position);
                    if (charIndex < 0) {
                        continue;
                    }
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