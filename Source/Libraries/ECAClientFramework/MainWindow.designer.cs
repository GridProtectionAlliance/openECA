namespace ECAClientFramework
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.RootPanel = new System.Windows.Forms.Panel();
            this.StatusTabControl = new System.Windows.Forms.TabControl();
            this.AlgorithmTabPage = new System.Windows.Forms.TabPage();
            this.AlgorithmMessageBox = new System.Windows.Forms.RichTextBox();
            this.AlgorithmMessageLabel = new System.Windows.Forms.Label();
            this.SubscriberTabPage = new System.Windows.Forms.TabPage();
            this.SubscriberSplitContainer = new System.Windows.Forms.SplitContainer();
            this.SubscriberStatusGroupBox = new System.Windows.Forms.GroupBox();
            this.SubscriberStatusBox = new System.Windows.Forms.RichTextBox();
            this.SubscriberStatusLabel = new System.Windows.Forms.Label();
            this.SubscriberMessageGroupBox = new System.Windows.Forms.GroupBox();
            this.SubscriberMessageBox = new System.Windows.Forms.RichTextBox();
            this.SubscriberMessageLabel = new System.Windows.Forms.Label();
            this.ConcentratorTabPage = new System.Windows.Forms.TabPage();
            this.ConcentratorSplitContainer = new System.Windows.Forms.SplitContainer();
            this.ConcentratorStatusGroupBox = new System.Windows.Forms.GroupBox();
            this.ConcentratorStatusBox = new System.Windows.Forms.RichTextBox();
            this.ConcentratorStatusLabel = new System.Windows.Forms.Label();
            this.ConcentratorMessageGroupBox = new System.Windows.Forms.GroupBox();
            this.ConcentratorMessageBox = new System.Windows.Forms.RichTextBox();
            this.ConcentratorMessageLabel = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.RootPanel.SuspendLayout();
            this.StatusTabControl.SuspendLayout();
            this.AlgorithmTabPage.SuspendLayout();
            this.SubscriberTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SubscriberSplitContainer)).BeginInit();
            this.SubscriberSplitContainer.Panel1.SuspendLayout();
            this.SubscriberSplitContainer.Panel2.SuspendLayout();
            this.SubscriberSplitContainer.SuspendLayout();
            this.SubscriberStatusGroupBox.SuspendLayout();
            this.SubscriberMessageGroupBox.SuspendLayout();
            this.ConcentratorTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ConcentratorSplitContainer)).BeginInit();
            this.ConcentratorSplitContainer.Panel1.SuspendLayout();
            this.ConcentratorSplitContainer.Panel2.SuspendLayout();
            this.ConcentratorSplitContainer.SuspendLayout();
            this.ConcentratorStatusGroupBox.SuspendLayout();
            this.ConcentratorMessageGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // RootPanel
            // 
            this.RootPanel.Controls.Add(this.StatusTabControl);
            this.RootPanel.Controls.Add(this.panel1);
            this.RootPanel.Controls.Add(this.label1);
            this.RootPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RootPanel.Location = new System.Drawing.Point(0, 0);
            this.RootPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.RootPanel.Name = "RootPanel";
            this.RootPanel.Padding = new System.Windows.Forms.Padding(15, 15, 15, 15);
            this.RootPanel.Size = new System.Drawing.Size(1101, 706);
            this.RootPanel.TabIndex = 1;
            // 
            // StatusTabControl
            // 
            this.StatusTabControl.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.StatusTabControl.Controls.Add(this.AlgorithmTabPage);
            this.StatusTabControl.Controls.Add(this.SubscriberTabPage);
            this.StatusTabControl.Controls.Add(this.ConcentratorTabPage);
            this.StatusTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusTabControl.Location = new System.Drawing.Point(15, 65);
            this.StatusTabControl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.StatusTabControl.Name = "StatusTabControl";
            this.StatusTabControl.SelectedIndex = 0;
            this.StatusTabControl.Size = new System.Drawing.Size(1071, 626);
            this.StatusTabControl.TabIndex = 3;
            // 
            // AlgorithmTabPage
            // 
            this.AlgorithmTabPage.Controls.Add(this.AlgorithmMessageBox);
            this.AlgorithmTabPage.Controls.Add(this.AlgorithmMessageLabel);
            this.AlgorithmTabPage.Location = new System.Drawing.Point(4, 4);
            this.AlgorithmTabPage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.AlgorithmTabPage.Name = "AlgorithmTabPage";
            this.AlgorithmTabPage.Size = new System.Drawing.Size(1063, 593);
            this.AlgorithmTabPage.TabIndex = 2;
            this.AlgorithmTabPage.Text = "Algorithm";
            this.AlgorithmTabPage.UseVisualStyleBackColor = true;
            // 
            // AlgorithmMessageBox
            // 
            this.AlgorithmMessageBox.BackColor = System.Drawing.SystemColors.Window;
            this.AlgorithmMessageBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AlgorithmMessageBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AlgorithmMessageBox.Location = new System.Drawing.Point(0, 20);
            this.AlgorithmMessageBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.AlgorithmMessageBox.Name = "AlgorithmMessageBox";
            this.AlgorithmMessageBox.ReadOnly = true;
            this.AlgorithmMessageBox.Size = new System.Drawing.Size(1063, 573);
            this.AlgorithmMessageBox.TabIndex = 0;
            this.AlgorithmMessageBox.Text = "";
            // 
            // AlgorithmMessageLabel
            // 
            this.AlgorithmMessageLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.AlgorithmMessageLabel.ForeColor = System.Drawing.Color.Blue;
            this.AlgorithmMessageLabel.Location = new System.Drawing.Point(0, 0);
            this.AlgorithmMessageLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.AlgorithmMessageLabel.Name = "AlgorithmMessageLabel";
            this.AlgorithmMessageLabel.Size = new System.Drawing.Size(1063, 20);
            this.AlgorithmMessageLabel.TabIndex = 1;
            this.AlgorithmMessageLabel.Text = "Message updates paused!";
            this.AlgorithmMessageLabel.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            this.AlgorithmMessageLabel.Visible = false;
            // 
            // SubscriberTabPage
            // 
            this.SubscriberTabPage.Controls.Add(this.SubscriberSplitContainer);
            this.SubscriberTabPage.Location = new System.Drawing.Point(4, 4);
            this.SubscriberTabPage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SubscriberTabPage.Name = "SubscriberTabPage";
            this.SubscriberTabPage.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SubscriberTabPage.Size = new System.Drawing.Size(1063, 592);
            this.SubscriberTabPage.TabIndex = 0;
            this.SubscriberTabPage.Text = "Subscriber";
            this.SubscriberTabPage.UseVisualStyleBackColor = true;
            // 
            // SubscriberSplitContainer
            // 
            this.SubscriberSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubscriberSplitContainer.Location = new System.Drawing.Point(4, 5);
            this.SubscriberSplitContainer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SubscriberSplitContainer.Name = "SubscriberSplitContainer";
            // 
            // SubscriberSplitContainer.Panel1
            // 
            this.SubscriberSplitContainer.Panel1.Controls.Add(this.SubscriberStatusGroupBox);
            // 
            // SubscriberSplitContainer.Panel2
            // 
            this.SubscriberSplitContainer.Panel2.Controls.Add(this.SubscriberMessageGroupBox);
            this.SubscriberSplitContainer.Size = new System.Drawing.Size(1055, 582);
            this.SubscriberSplitContainer.SplitterDistance = 527;
            this.SubscriberSplitContainer.SplitterWidth = 3;
            this.SubscriberSplitContainer.TabIndex = 1;
            // 
            // SubscriberStatusGroupBox
            // 
            this.SubscriberStatusGroupBox.Controls.Add(this.SubscriberStatusBox);
            this.SubscriberStatusGroupBox.Controls.Add(this.SubscriberStatusLabel);
            this.SubscriberStatusGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubscriberStatusGroupBox.Location = new System.Drawing.Point(0, 0);
            this.SubscriberStatusGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SubscriberStatusGroupBox.Name = "SubscriberStatusGroupBox";
            this.SubscriberStatusGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SubscriberStatusGroupBox.Size = new System.Drawing.Size(527, 582);
            this.SubscriberStatusGroupBox.TabIndex = 1;
            this.SubscriberStatusGroupBox.TabStop = false;
            this.SubscriberStatusGroupBox.Text = "Status";
            // 
            // SubscriberStatusBox
            // 
            this.SubscriberStatusBox.BackColor = System.Drawing.SystemColors.Window;
            this.SubscriberStatusBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubscriberStatusBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubscriberStatusBox.Location = new System.Drawing.Point(4, 44);
            this.SubscriberStatusBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SubscriberStatusBox.Name = "SubscriberStatusBox";
            this.SubscriberStatusBox.ReadOnly = true;
            this.SubscriberStatusBox.Size = new System.Drawing.Size(519, 533);
            this.SubscriberStatusBox.TabIndex = 0;
            this.SubscriberStatusBox.Text = "";
            this.SubscriberStatusBox.WordWrap = false;
            // 
            // SubscriberStatusLabel
            // 
            this.SubscriberStatusLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.SubscriberStatusLabel.ForeColor = System.Drawing.Color.Blue;
            this.SubscriberStatusLabel.Location = new System.Drawing.Point(4, 24);
            this.SubscriberStatusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.SubscriberStatusLabel.Name = "SubscriberStatusLabel";
            this.SubscriberStatusLabel.Size = new System.Drawing.Size(519, 20);
            this.SubscriberStatusLabel.TabIndex = 2;
            this.SubscriberStatusLabel.Text = "Message updates paused!";
            this.SubscriberStatusLabel.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            this.SubscriberStatusLabel.Visible = false;
            // 
            // SubscriberMessageGroupBox
            // 
            this.SubscriberMessageGroupBox.Controls.Add(this.SubscriberMessageBox);
            this.SubscriberMessageGroupBox.Controls.Add(this.SubscriberMessageLabel);
            this.SubscriberMessageGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubscriberMessageGroupBox.Location = new System.Drawing.Point(0, 0);
            this.SubscriberMessageGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SubscriberMessageGroupBox.Name = "SubscriberMessageGroupBox";
            this.SubscriberMessageGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SubscriberMessageGroupBox.Size = new System.Drawing.Size(525, 582);
            this.SubscriberMessageGroupBox.TabIndex = 1;
            this.SubscriberMessageGroupBox.TabStop = false;
            this.SubscriberMessageGroupBox.Text = "Messages";
            // 
            // SubscriberMessageBox
            // 
            this.SubscriberMessageBox.BackColor = System.Drawing.SystemColors.Window;
            this.SubscriberMessageBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubscriberMessageBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubscriberMessageBox.Location = new System.Drawing.Point(4, 44);
            this.SubscriberMessageBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.SubscriberMessageBox.Name = "SubscriberMessageBox";
            this.SubscriberMessageBox.ReadOnly = true;
            this.SubscriberMessageBox.Size = new System.Drawing.Size(517, 533);
            this.SubscriberMessageBox.TabIndex = 0;
            this.SubscriberMessageBox.Text = "";
            this.SubscriberMessageBox.WordWrap = false;
            // 
            // SubscriberMessageLabel
            // 
            this.SubscriberMessageLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.SubscriberMessageLabel.ForeColor = System.Drawing.Color.Blue;
            this.SubscriberMessageLabel.Location = new System.Drawing.Point(4, 24);
            this.SubscriberMessageLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.SubscriberMessageLabel.Name = "SubscriberMessageLabel";
            this.SubscriberMessageLabel.Size = new System.Drawing.Size(517, 20);
            this.SubscriberMessageLabel.TabIndex = 3;
            this.SubscriberMessageLabel.Text = "Message updates paused!";
            this.SubscriberMessageLabel.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            this.SubscriberMessageLabel.Visible = false;
            // 
            // ConcentratorTabPage
            // 
            this.ConcentratorTabPage.Controls.Add(this.ConcentratorSplitContainer);
            this.ConcentratorTabPage.Location = new System.Drawing.Point(4, 4);
            this.ConcentratorTabPage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ConcentratorTabPage.Name = "ConcentratorTabPage";
            this.ConcentratorTabPage.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ConcentratorTabPage.Size = new System.Drawing.Size(1063, 592);
            this.ConcentratorTabPage.TabIndex = 1;
            this.ConcentratorTabPage.Text = "Concentrator";
            this.ConcentratorTabPage.UseVisualStyleBackColor = true;
            // 
            // ConcentratorSplitContainer
            // 
            this.ConcentratorSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConcentratorSplitContainer.Location = new System.Drawing.Point(4, 5);
            this.ConcentratorSplitContainer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ConcentratorSplitContainer.Name = "ConcentratorSplitContainer";
            // 
            // ConcentratorSplitContainer.Panel1
            // 
            this.ConcentratorSplitContainer.Panel1.Controls.Add(this.ConcentratorStatusGroupBox);
            // 
            // ConcentratorSplitContainer.Panel2
            // 
            this.ConcentratorSplitContainer.Panel2.Controls.Add(this.ConcentratorMessageGroupBox);
            this.ConcentratorSplitContainer.Size = new System.Drawing.Size(1055, 582);
            this.ConcentratorSplitContainer.SplitterDistance = 527;
            this.ConcentratorSplitContainer.SplitterWidth = 3;
            this.ConcentratorSplitContainer.TabIndex = 1;
            // 
            // ConcentratorStatusGroupBox
            // 
            this.ConcentratorStatusGroupBox.Controls.Add(this.ConcentratorStatusBox);
            this.ConcentratorStatusGroupBox.Controls.Add(this.ConcentratorStatusLabel);
            this.ConcentratorStatusGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConcentratorStatusGroupBox.Location = new System.Drawing.Point(0, 0);
            this.ConcentratorStatusGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ConcentratorStatusGroupBox.Name = "ConcentratorStatusGroupBox";
            this.ConcentratorStatusGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ConcentratorStatusGroupBox.Size = new System.Drawing.Size(527, 582);
            this.ConcentratorStatusGroupBox.TabIndex = 0;
            this.ConcentratorStatusGroupBox.TabStop = false;
            this.ConcentratorStatusGroupBox.Text = "Status";
            // 
            // ConcentratorStatusBox
            // 
            this.ConcentratorStatusBox.BackColor = System.Drawing.SystemColors.Window;
            this.ConcentratorStatusBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConcentratorStatusBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConcentratorStatusBox.Location = new System.Drawing.Point(4, 44);
            this.ConcentratorStatusBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ConcentratorStatusBox.Name = "ConcentratorStatusBox";
            this.ConcentratorStatusBox.ReadOnly = true;
            this.ConcentratorStatusBox.Size = new System.Drawing.Size(519, 533);
            this.ConcentratorStatusBox.TabIndex = 0;
            this.ConcentratorStatusBox.Text = "";
            this.ConcentratorStatusBox.WordWrap = false;
            // 
            // ConcentratorStatusLabel
            // 
            this.ConcentratorStatusLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ConcentratorStatusLabel.ForeColor = System.Drawing.Color.Blue;
            this.ConcentratorStatusLabel.Location = new System.Drawing.Point(4, 24);
            this.ConcentratorStatusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ConcentratorStatusLabel.Name = "ConcentratorStatusLabel";
            this.ConcentratorStatusLabel.Size = new System.Drawing.Size(519, 20);
            this.ConcentratorStatusLabel.TabIndex = 4;
            this.ConcentratorStatusLabel.Text = "Message updates paused!";
            this.ConcentratorStatusLabel.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            this.ConcentratorStatusLabel.Visible = false;
            // 
            // ConcentratorMessageGroupBox
            // 
            this.ConcentratorMessageGroupBox.Controls.Add(this.ConcentratorMessageBox);
            this.ConcentratorMessageGroupBox.Controls.Add(this.ConcentratorMessageLabel);
            this.ConcentratorMessageGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConcentratorMessageGroupBox.Location = new System.Drawing.Point(0, 0);
            this.ConcentratorMessageGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ConcentratorMessageGroupBox.Name = "ConcentratorMessageGroupBox";
            this.ConcentratorMessageGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ConcentratorMessageGroupBox.Size = new System.Drawing.Size(525, 582);
            this.ConcentratorMessageGroupBox.TabIndex = 0;
            this.ConcentratorMessageGroupBox.TabStop = false;
            this.ConcentratorMessageGroupBox.Text = "Messages";
            // 
            // ConcentratorMessageBox
            // 
            this.ConcentratorMessageBox.BackColor = System.Drawing.SystemColors.Window;
            this.ConcentratorMessageBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConcentratorMessageBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConcentratorMessageBox.Location = new System.Drawing.Point(4, 44);
            this.ConcentratorMessageBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ConcentratorMessageBox.Name = "ConcentratorMessageBox";
            this.ConcentratorMessageBox.ReadOnly = true;
            this.ConcentratorMessageBox.Size = new System.Drawing.Size(517, 533);
            this.ConcentratorMessageBox.TabIndex = 0;
            this.ConcentratorMessageBox.Text = "";
            this.ConcentratorMessageBox.WordWrap = false;
            // 
            // ConcentratorMessageLabel
            // 
            this.ConcentratorMessageLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ConcentratorMessageLabel.ForeColor = System.Drawing.Color.Blue;
            this.ConcentratorMessageLabel.Location = new System.Drawing.Point(4, 24);
            this.ConcentratorMessageLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ConcentratorMessageLabel.Name = "ConcentratorMessageLabel";
            this.ConcentratorMessageLabel.Size = new System.Drawing.Size(517, 20);
            this.ConcentratorMessageLabel.TabIndex = 5;
            this.ConcentratorMessageLabel.Text = "Message updates paused!";
            this.ConcentratorMessageLabel.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            this.ConcentratorMessageLabel.Visible = false;
            // 
            // panel1
            // 
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(15, 50);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1071, 15);
            this.panel1.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(15, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1071, 35);
            this.label1.TabIndex = 1;
            this.label1.Text = "AlgorithmTemplate is running. To stop the algorithm, close this window.";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1101, 706);
            this.Controls.Add(this.RootPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MainWindow";
            this.Text = "AlgorithmTemplate";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWindow_FormClosed);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.RootPanel.ResumeLayout(false);
            this.StatusTabControl.ResumeLayout(false);
            this.AlgorithmTabPage.ResumeLayout(false);
            this.SubscriberTabPage.ResumeLayout(false);
            this.SubscriberSplitContainer.Panel1.ResumeLayout(false);
            this.SubscriberSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SubscriberSplitContainer)).EndInit();
            this.SubscriberSplitContainer.ResumeLayout(false);
            this.SubscriberStatusGroupBox.ResumeLayout(false);
            this.SubscriberMessageGroupBox.ResumeLayout(false);
            this.ConcentratorTabPage.ResumeLayout(false);
            this.ConcentratorSplitContainer.Panel1.ResumeLayout(false);
            this.ConcentratorSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ConcentratorSplitContainer)).EndInit();
            this.ConcentratorSplitContainer.ResumeLayout(false);
            this.ConcentratorStatusGroupBox.ResumeLayout(false);
            this.ConcentratorMessageGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel RootPanel;
        private System.Windows.Forms.TabControl StatusTabControl;
        private System.Windows.Forms.TabPage SubscriberTabPage;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage ConcentratorTabPage;
        private System.Windows.Forms.RichTextBox SubscriberStatusBox;
        private System.Windows.Forms.RichTextBox ConcentratorStatusBox;
        private System.Windows.Forms.TabPage AlgorithmTabPage;
        private System.Windows.Forms.RichTextBox AlgorithmMessageBox;
        private System.Windows.Forms.SplitContainer SubscriberSplitContainer;
        private System.Windows.Forms.RichTextBox SubscriberMessageBox;
        private System.Windows.Forms.SplitContainer ConcentratorSplitContainer;
        private System.Windows.Forms.GroupBox ConcentratorStatusGroupBox;
        private System.Windows.Forms.GroupBox ConcentratorMessageGroupBox;
        private System.Windows.Forms.GroupBox SubscriberStatusGroupBox;
        private System.Windows.Forms.GroupBox SubscriberMessageGroupBox;
        private System.Windows.Forms.RichTextBox ConcentratorMessageBox;
        private System.Windows.Forms.Label AlgorithmMessageLabel;
        private System.Windows.Forms.Label SubscriberStatusLabel;
        private System.Windows.Forms.Label SubscriberMessageLabel;
        private System.Windows.Forms.Label ConcentratorStatusLabel;
        private System.Windows.Forms.Label ConcentratorMessageLabel;
    }
}

