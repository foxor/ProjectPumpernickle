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
            btnGroupByReward = new Button();
            btnGroupByPath = new Button();
            btnUngroup = new Button();
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
            // btnGroupByReward
            // 
            btnGroupByReward.Location = new Point(12, 654);
            btnGroupByReward.Name = "btnGroupByReward";
            btnGroupByReward.Size = new Size(176, 34);
            btnGroupByReward.TabIndex = 1;
            btnGroupByReward.Text = "Group by reward";
            btnGroupByReward.UseVisualStyleBackColor = true;
            btnGroupByReward.Click += groupByReward;
            // 
            // btnGroupByPath
            // 
            btnGroupByPath.Location = new Point(210, 654);
            btnGroupByPath.Name = "btnGroupByPath";
            btnGroupByPath.Size = new Size(148, 34);
            btnGroupByPath.TabIndex = 2;
            btnGroupByPath.Text = "Group By Path";
            btnGroupByPath.UseVisualStyleBackColor = true;
            btnGroupByPath.Click += groupByPath;
            // 
            // btnUngroup
            // 
            btnUngroup.Location = new Point(383, 654);
            btnUngroup.Name = "btnUngroup";
            btnUngroup.Size = new Size(112, 34);
            btnUngroup.TabIndex = 3;
            btnUngroup.Text = "Ungroup";
            btnUngroup.UseVisualStyleBackColor = true;
            btnUngroup.Click += groupByNothing;
            // 
            // PumpernickelExplanation
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(507, 700);
            Controls.Add(btnUngroup);
            Controls.Add(btnGroupByPath);
            Controls.Add(btnGroupByReward);
            Controls.Add(explanation);
            Name = "PumpernickelExplanation";
            Text = "PumpernickelExplanation";
            ResumeLayout(false);
        }

        #endregion

        public RichTextBox explanation;
        private Button btnGroupByReward;
        private Button btnGroupByPath;
        private Button btnUngroup;
    }
}