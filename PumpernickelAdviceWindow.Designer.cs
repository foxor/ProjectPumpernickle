namespace ProjectPumpernickle {
    partial class PumpernickelAdviceWindow {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            pathing = new TextBox();
            textBox1 = new TextBox();
            SuspendLayout();
            // 
            // pathing
            // 
            pathing.Enabled = false;
            pathing.Location = new Point(1275, 44);
            pathing.Multiline = true;
            pathing.Name = "pathing";
            pathing.Size = new Size(281, 993);
            pathing.TabIndex = 0;
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.None;
            textBox1.Location = new Point(12, 12);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.RightToLeft = RightToLeft.Yes;
            textBox1.Size = new Size(318, 162);
            textBox1.TabIndex = 1;
            textBox1.TextAlign = HorizontalAlignment.Center;
            // 
            // PumpernickelAdviceWindow
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(347, 188);
            Controls.Add(textBox1);
            Controls.Add(pathing);
            Name = "PumpernickelAdviceWindow";
            Text = "Project Pumpernickel";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        public TextBox pathing;
        private TextBox textBox1;
    }
}