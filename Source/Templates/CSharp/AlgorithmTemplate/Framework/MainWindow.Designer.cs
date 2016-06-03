namespace AlgorithmTemplate.Framework
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
            this.RootPanel = new System.Windows.Forms.Panel();
            this.StatusTabControl = new System.Windows.Forms.TabControl();
            this.SubscriberTabPage = new System.Windows.Forms.TabPage();
            this.SubscriberStatusBox = new System.Windows.Forms.RichTextBox();
            this.ConcentratorTabPage = new System.Windows.Forms.TabPage();
            this.ConcentratorStatusBox = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.RootPanel.SuspendLayout();
            this.StatusTabControl.SuspendLayout();
            this.SubscriberTabPage.SuspendLayout();
            this.ConcentratorTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // RootPanel
            // 
            this.RootPanel.Controls.Add(this.StatusTabControl);
            this.RootPanel.Controls.Add(this.panel1);
            this.RootPanel.Controls.Add(this.label1);
            this.RootPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RootPanel.Location = new System.Drawing.Point(0, 0);
            this.RootPanel.Name = "RootPanel";
            this.RootPanel.Padding = new System.Windows.Forms.Padding(10);
            this.RootPanel.Size = new System.Drawing.Size(748, 439);
            this.RootPanel.TabIndex = 1;
            // 
            // StatusTabControl
            // 
            this.StatusTabControl.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.StatusTabControl.Controls.Add(this.SubscriberTabPage);
            this.StatusTabControl.Controls.Add(this.ConcentratorTabPage);
            this.StatusTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusTabControl.Location = new System.Drawing.Point(10, 43);
            this.StatusTabControl.Name = "StatusTabControl";
            this.StatusTabControl.SelectedIndex = 0;
            this.StatusTabControl.Size = new System.Drawing.Size(728, 386);
            this.StatusTabControl.TabIndex = 3;
            // 
            // SubscriberTabPage
            // 
            this.SubscriberTabPage.Controls.Add(this.SubscriberStatusBox);
            this.SubscriberTabPage.Location = new System.Drawing.Point(4, 4);
            this.SubscriberTabPage.Name = "SubscriberTabPage";
            this.SubscriberTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.SubscriberTabPage.Size = new System.Drawing.Size(720, 360);
            this.SubscriberTabPage.TabIndex = 0;
            this.SubscriberTabPage.Text = "Subscriber";
            this.SubscriberTabPage.UseVisualStyleBackColor = true;
            // 
            // SubscriberStatusBox
            // 
            this.SubscriberStatusBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubscriberStatusBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubscriberStatusBox.Location = new System.Drawing.Point(3, 3);
            this.SubscriberStatusBox.Name = "SubscriberStatusBox";
            this.SubscriberStatusBox.ReadOnly = true;
            this.SubscriberStatusBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.SubscriberStatusBox.Size = new System.Drawing.Size(714, 354);
            this.SubscriberStatusBox.TabIndex = 0;
            this.SubscriberStatusBox.Text = "";
            this.SubscriberStatusBox.SelectionChanged += new System.EventHandler(this.StatusBox_SelectionChanged);
            // 
            // ConcentratorTabPage
            // 
            this.ConcentratorTabPage.Controls.Add(this.ConcentratorStatusBox);
            this.ConcentratorTabPage.Location = new System.Drawing.Point(4, 4);
            this.ConcentratorTabPage.Name = "ConcentratorTabPage";
            this.ConcentratorTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.ConcentratorTabPage.Size = new System.Drawing.Size(720, 360);
            this.ConcentratorTabPage.TabIndex = 1;
            this.ConcentratorTabPage.Text = "Concentrator";
            this.ConcentratorTabPage.UseVisualStyleBackColor = true;
            // 
            // ConcentratorStatusBox
            // 
            this.ConcentratorStatusBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConcentratorStatusBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConcentratorStatusBox.Location = new System.Drawing.Point(3, 3);
            this.ConcentratorStatusBox.Name = "ConcentratorStatusBox";
            this.ConcentratorStatusBox.ReadOnly = true;
            this.ConcentratorStatusBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.ConcentratorStatusBox.Size = new System.Drawing.Size(714, 354);
            this.ConcentratorStatusBox.TabIndex = 0;
            this.ConcentratorStatusBox.Text = "";
            this.ConcentratorStatusBox.SelectionChanged += new System.EventHandler(this.StatusBox_SelectionChanged);
            // 
            // panel1
            // 
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(10, 33);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(728, 10);
            this.panel1.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(10, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(728, 23);
            this.label1.TabIndex = 1;
            this.label1.Text = "AlgorithmTemplate is running. To stop the algorithm, close this window.";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(748, 439);
            this.Controls.Add(this.RootPanel);
            this.Name = "MainWindow";
            this.Text = "AlgorithmTemplate";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWindow_FormClosed);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.RootPanel.ResumeLayout(false);
            this.StatusTabControl.ResumeLayout(false);
            this.SubscriberTabPage.ResumeLayout(false);
            this.ConcentratorTabPage.ResumeLayout(false);
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
    }
}

