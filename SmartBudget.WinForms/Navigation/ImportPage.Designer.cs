namespace SmartBudget.WinForms.Navigation
{
    partial class ImportPage
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            splitContainer1 = new SplitContainer();
            fileDropZone1 = new SmartBudget.WinForms.Controls.FileDrop.FileDropZone();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(fileDropZone1);
            splitContainer1.Size = new Size(2195, 1006);
            splitContainer1.SplitterDistance = 1875;
            splitContainer1.TabIndex = 0;
            // 
            // fileDropZone1
            // 
            fileDropZone1.AllowDrop = true;
            fileDropZone1.BackColor = Color.FromArgb(214, 230, 248);
            fileDropZone1.Dock = DockStyle.Fill;
            fileDropZone1.Location = new Point(0, 0);
            fileDropZone1.Name = "fileDropZone1";
            fileDropZone1.Size = new Size(316, 1006);
            fileDropZone1.TabIndex = 0;
            // 
            // ImportPage
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Name = "ImportPage";
            Size = new Size(2195, 1006);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private Controls.FileDrop.FileDropZone fileDropZone1;
    }
}
