namespace openECAClient
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
            this.components = new System.ComponentModel.Container();
            this.MainWindowMenuStrip = new System.Windows.Forms.MenuStrip();
            this.FileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CloseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RootPanel = new System.Windows.Forms.Panel();
            this.MessagesTextBox = new System.Windows.Forms.RichTextBox();
            this.ErrorLogger = new GSF.ErrorManagement.ErrorLogger(this.components);
            this.MainWindowMenuStrip.SuspendLayout();
            this.RootPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ErrorLogger)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ErrorLogger.ErrorLog)).BeginInit();
            this.SuspendLayout();
            // 
            // MainWindowMenuStrip
            // 
            this.MainWindowMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileToolStripMenuItem,
            this.OptionsToolStripMenuItem});
            this.MainWindowMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MainWindowMenuStrip.Name = "MainWindowMenuStrip";
            this.MainWindowMenuStrip.Size = new System.Drawing.Size(698, 24);
            this.MainWindowMenuStrip.TabIndex = 0;
            this.MainWindowMenuStrip.Text = "menuStrip1";
            // 
            // FileToolStripMenuItem
            // 
            this.FileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CloseToolStripMenuItem});
            this.FileToolStripMenuItem.Name = "FileToolStripMenuItem";
            this.FileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.FileToolStripMenuItem.Text = "File";
            // 
            // CloseToolStripMenuItem
            // 
            this.CloseToolStripMenuItem.Name = "CloseToolStripMenuItem";
            this.CloseToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.CloseToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.CloseToolStripMenuItem.Text = "Close";
            this.CloseToolStripMenuItem.Click += new System.EventHandler(this.CloseToolStripMenuItem_Click);
            // 
            // OptionsToolStripMenuItem
            // 
            this.OptionsToolStripMenuItem.Name = "OptionsToolStripMenuItem";
            this.OptionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.OptionsToolStripMenuItem.Text = "Options";
            // 
            // RootPanel
            // 
            this.RootPanel.Controls.Add(this.MessagesTextBox);
            this.RootPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RootPanel.Location = new System.Drawing.Point(0, 24);
            this.RootPanel.Name = "RootPanel";
            this.RootPanel.Padding = new System.Windows.Forms.Padding(10);
            this.RootPanel.Size = new System.Drawing.Size(698, 381);
            this.RootPanel.TabIndex = 2;
            // 
            // MessagesTextBox
            // 
            this.MessagesTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MessagesTextBox.Location = new System.Drawing.Point(10, 10);
            this.MessagesTextBox.Name = "MessagesTextBox";
            this.MessagesTextBox.Size = new System.Drawing.Size(678, 361);
            this.MessagesTextBox.TabIndex = 0;
            this.MessagesTextBox.Text = "";
            // 
            // ErrorLogger
            // 
            // 
            // 
            // 
            this.ErrorLogger.ErrorLog.FileName = "ErrorLog.txt";
            this.ErrorLogger.ErrorLog.PersistSettings = true;
            this.ErrorLogger.LogToEventLog = false;
            this.ErrorLogger.PersistSettings = true;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(698, 405);
            this.Controls.Add(this.RootPanel);
            this.Controls.Add(this.MainWindowMenuStrip);
            this.MainMenuStrip = this.MainWindowMenuStrip;
            this.Name = "MainWindow";
            this.Text = "openECA Client";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWindow_FormClosed);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.MainWindowMenuStrip.ResumeLayout(false);
            this.MainWindowMenuStrip.PerformLayout();
            this.RootPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ErrorLogger.ErrorLog)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ErrorLogger)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip MainWindowMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem FileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OptionsToolStripMenuItem;
        private System.Windows.Forms.Panel RootPanel;
        private System.Windows.Forms.ToolStripMenuItem CloseToolStripMenuItem;
        private System.Windows.Forms.RichTextBox MessagesTextBox;
        private GSF.ErrorManagement.ErrorLogger ErrorLogger;
    }
}

