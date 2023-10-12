using System.Runtime.CompilerServices;

namespace ProjectPumpernickle {
    public partial class PumpernickelAdviceWindow : Form {
        protected static PumpernickelAdviceWindow instance = null;
        public PumpernickelAdviceWindow() {
            InitializeComponent();
            instance = this;
        }

        private void Form1_Load(object sender, EventArgs e) {

        }
        public static void SetText(string text) {
            instance.textBox1.Text = text;
        }
    }
}