using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ProjectPumpernickle {
    public partial class PumpernickelAdviceWindow : Form {
        public static PumpernickelAdviceWindow instance = null;
        protected int lastHoveredIndex = -1;
        protected string[] PathTexts = new string[0];
        protected System.Drawing.Color defaultColor;
        protected Evaluation chosenEvaluation;

        public PumpernickelAdviceWindow() {
            InitializeComponent();
            instance = this;
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
            var hoveredCharIndex = PathPreview.GetCharIndexFromPosition(e.Location);
            if (hoveredCharIndex != lastHoveredIndex) {
                var asCoord = IndexToPosition(hoveredCharIndex, out var isValid);
                isValid &= PathPreview.Text[hoveredCharIndex] != ' ';
                if (isValid && chosenEvaluation?.Path != null) {
                    lastHoveredIndex = hoveredCharIndex;
                    var pathIndex = chosenEvaluation.Path.nodes.FirstIndexOf(x => x.position.Equals(asCoord));
                    if (pathIndex != -1) {
                        PathNodeInfoBox.Text =
                            "Expected Health: " + chosenEvaluation.Path.expectedHealth[pathIndex] + "\n" +
                            "Worst Case Health: " + chosenEvaluation.Path.worstCaseHealth[pathIndex] + "\n" +
                            "Expected Gold: " + chosenEvaluation.Path.expectedGold[pathIndex] + "\n"
                        ;
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

        public void SetEvaluation(Evaluation evaluation) {
            chosenEvaluation = evaluation;
            instance.AdviceBox.Text = evaluation.ToString();
            if (evaluation.Path != null) {
                foreach (var pathNode in evaluation.Path.nodes) {
                    var charIndex = PositionToIndex(pathNode.position);
                    PathPreview.SelectionStart = charIndex;
                    PathPreview.SelectionLength = 1;
                    PathPreview.SelectionColor = System.Drawing.Color.Red;
                    // Does this need to get set back?
                }
            }
        }
    }
}