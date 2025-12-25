namespace SmartBudget.WinForms.Navigation
{
    partial class Dashboard
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
            balanceTile = new SmartBudget.WinForms.Controls.Balance.BalanceTile();
            SuspendLayout();
            // 
            // balanceTile
            // 
            balanceTile.BackColor = Color.Transparent;
            balanceTile.Font = new Font("Segoe UI", 9F);
            balanceTile.ForeColor = Color.White;
            balanceTile.Location = new Point(3, 3);
            balanceTile.MinimumSize = new Size(240, 120);
            balanceTile.Name = "balanceTile";
            balanceTile.Padding = new Padding(16);
            balanceTile.Size = new Size(425, 175);
            balanceTile.TabIndex = 0;
            // 
            // Dashboard
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(balanceTile);
            Name = "Dashboard";
            Size = new Size(1767, 1043);
            ResumeLayout(false);
        }

        #endregion

        private Controls.Balance.BalanceTile balanceTile;
    }
}
