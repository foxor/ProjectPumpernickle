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
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(PumpernickelAdviceWindow));
            pathing = new TextBox();
            AdviceBox = new TextBox();
            PathPreview = new RichTextBox();
            PathNodeInfoBox = new RichTextBox();
            whyButton = new Button();
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
            // AdviceBox
            // 
            AdviceBox.Location = new Point(281, 392);
            AdviceBox.Multiline = true;
            AdviceBox.Name = "AdviceBox";
            AdviceBox.RightToLeft = RightToLeft.Yes;
            AdviceBox.ScrollBars = ScrollBars.Vertical;
            AdviceBox.Size = new Size(243, 232);
            AdviceBox.TabIndex = 1;
            AdviceBox.TextAlign = HorizontalAlignment.Center;
            // 
            // PathPreview
            // 
            PathPreview.Font = new Font("Courier New", 10F, FontStyle.Regular, GraphicsUnit.Point);
            PathPreview.ForeColor = SystemColors.GrayText;
            PathPreview.Location = new Point(12, 12);
            PathPreview.Name = "PathPreview";
            PathPreview.Size = new Size(263, 671);
            PathPreview.TabIndex = 2;
            PathPreview.Text = resources.GetString("PathPreview.Text");
            // 
            // PathNodeInfoBox
            // 
            PathNodeInfoBox.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point);
            PathNodeInfoBox.Location = new Point(281, 12);
            PathNodeInfoBox.Name = "PathNodeInfoBox";
            PathNodeInfoBox.Size = new Size(243, 374);
            PathNodeInfoBox.TabIndex = 3;
            PathNodeInfoBox.Text = "";
            // 
            // whyButton
            // 
            whyButton.Location = new Point(281, 630);
            whyButton.Name = "whyButton";
            whyButton.Size = new Size(243, 53);
            whyButton.TabIndex = 4;
            whyButton.Text = "Why?";
            whyButton.UseVisualStyleBackColor = true;
            // 
            // PumpernickelAdviceWindow
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(552, 706);
            Controls.Add(whyButton);
            Controls.Add(PathNodeInfoBox);
            Controls.Add(PathPreview);
            Controls.Add(AdviceBox);
            Controls.Add(pathing);
            Name = "PumpernickelAdviceWindow";
            Text = "Project Pumpernickel";
            Load += LoadForm;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        public TextBox pathing;
        private TextBox AdviceBox;
        private RichTextBox PathPreview;
        private RichTextBox PathNodeInfoBox;
        private Button whyButton;
    }
}