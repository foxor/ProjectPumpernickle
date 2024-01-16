namespace ProjectPumpernickle {
    partial class PumpernickelExplanation {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            explanation = new RichTextBox();
            groupByButton = new Button();
            SuspendLayout();
            // 
            // explanation
            // 
            explanation.Location = new Point(12, 12);
            explanation.Name = "explanation";
            explanation.Size = new Size(483, 636);
            explanation.TabIndex = 0;
            explanation.Text = "";
            // 
            // button1
            // 
            groupByButton.Location = new Point(12, 654);
            groupByButton.Name = "groupByButton";
            groupByButton.Size = new Size(176, 34);
            groupByButton.TabIndex = 1;
            groupByButton.Text = "Group by reward";
            groupByButton.UseVisualStyleBackColor = true;
            groupByButton.Click += groupByReward;
            // 
            // PumpernickelExplanation
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(507, 700);
            Controls.Add(groupByButton);
            Controls.Add(explanation);
            Name = "PumpernickelExplanation";
            Text = "PumpernickelExplanation";
            ResumeLayout(false);
        }

        #endregion

        public RichTextBox explanation;
        private Button groupByButton;
    }
}