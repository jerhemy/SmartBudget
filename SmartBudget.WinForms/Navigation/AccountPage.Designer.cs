namespace SmartBudget.WinForms.Navigation
{
    partial class AccountPage
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
            gridAutoPays = new DataGridView();
            miniBalanceLineChart1 = new SmartBudget.WinForms.Controls.Charts.MiniBalanceLineChart();
            btnImportQuicken = new Button();
            panel1 = new Panel();
            btnDeleteTransaction = new Button();
            txtAmount = new TextBox();
            txtTitle = new TextBox();
            btnAddTransaction = new Button();
            cboAccounts = new ComboBox();
            dtTransactionDate = new DateTimePicker();
            btnImportCsv = new Button();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            calendarControl = new SmartBudget.WinForms.Controls.Calendar.CalendarView();
            tabPage2 = new TabPage();
            dataGridView1 = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridAutoPays).BeginInit();
            panel1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(gridAutoPays);
            splitContainer1.Panel1.Controls.Add(miniBalanceLineChart1);
            splitContainer1.Panel1.Controls.Add(btnImportQuicken);
            splitContainer1.Panel1.Controls.Add(panel1);
            splitContainer1.Panel1.Controls.Add(btnImportCsv);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(tabControl1);
            splitContainer1.Size = new Size(2296, 1162);
            splitContainer1.SplitterDistance = 758;
            splitContainer1.TabIndex = 1;
            // 
            // gridAutoPays
            // 
            gridAutoPays.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridAutoPays.Dock = DockStyle.Fill;
            gridAutoPays.Location = new Point(0, 0);
            gridAutoPays.Name = "gridAutoPays";
            gridAutoPays.RowHeadersWidth = 51;
            gridAutoPays.Size = new Size(758, 1162);
            gridAutoPays.TabIndex = 5;
            // 
            // miniBalanceLineChart1
            // 
            miniBalanceLineChart1.Dock = DockStyle.Fill;
            miniBalanceLineChart1.Location = new Point(0, 0);
            miniBalanceLineChart1.Name = "miniBalanceLineChart1";
            miniBalanceLineChart1.Padding = new Padding(25);
            miniBalanceLineChart1.Size = new Size(758, 1162);
            miniBalanceLineChart1.TabIndex = 4;
            miniBalanceLineChart1.Text = "miniBalanceLineChart1";
            // 
            // btnImportQuicken
            // 
            btnImportQuicken.Dock = DockStyle.Fill;
            btnImportQuicken.Location = new Point(0, 0);
            btnImportQuicken.Name = "btnImportQuicken";
            btnImportQuicken.Size = new Size(758, 1162);
            btnImportQuicken.TabIndex = 3;
            btnImportQuicken.Text = "Import Quicken File";
            btnImportQuicken.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(btnDeleteTransaction);
            panel1.Controls.Add(txtAmount);
            panel1.Controls.Add(txtTitle);
            panel1.Controls.Add(btnAddTransaction);
            panel1.Controls.Add(cboAccounts);
            panel1.Controls.Add(dtTransactionDate);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(758, 1162);
            panel1.TabIndex = 2;
            // 
            // btnDeleteTransaction
            // 
            btnDeleteTransaction.Location = new Point(19, 268);
            btnDeleteTransaction.Name = "btnDeleteTransaction";
            btnDeleteTransaction.Size = new Size(180, 29);
            btnDeleteTransaction.TabIndex = 4;
            btnDeleteTransaction.Text = "Delete Transaction";
            btnDeleteTransaction.UseVisualStyleBackColor = true;
            // 
            // txtAmount
            // 
            txtAmount.BorderStyle = BorderStyle.FixedSingle;
            txtAmount.Location = new Point(19, 139);
            txtAmount.Name = "txtAmount";
            txtAmount.Size = new Size(125, 27);
            txtAmount.TabIndex = 3;
            // 
            // txtTitle
            // 
            txtTitle.BorderStyle = BorderStyle.FixedSingle;
            txtTitle.Location = new Point(19, 70);
            txtTitle.Multiline = true;
            txtTitle.Name = "txtTitle";
            txtTitle.Size = new Size(545, 52);
            txtTitle.TabIndex = 2;
            // 
            // btnAddTransaction
            // 
            btnAddTransaction.Location = new Point(384, 268);
            btnAddTransaction.Name = "btnAddTransaction";
            btnAddTransaction.Size = new Size(180, 29);
            btnAddTransaction.TabIndex = 1;
            btnAddTransaction.Text = "Add Transaction";
            btnAddTransaction.UseVisualStyleBackColor = true;
            // 
            // cboAccounts
            // 
            cboAccounts.FormattingEnabled = true;
            cboAccounts.Location = new Point(19, 16);
            cboAccounts.Name = "cboAccounts";
            cboAccounts.Size = new Size(151, 28);
            cboAccounts.TabIndex = 0;
            // 
            // dtTransactionDate
            // 
            dtTransactionDate.Location = new Point(314, 14);
            dtTransactionDate.Name = "dtTransactionDate";
            dtTransactionDate.Size = new Size(250, 27);
            dtTransactionDate.TabIndex = 0;
            // 
            // btnImportCsv
            // 
            btnImportCsv.Dock = DockStyle.Fill;
            btnImportCsv.Location = new Point(0, 0);
            btnImportCsv.Name = "btnImportCsv";
            btnImportCsv.Size = new Size(758, 1162);
            btnImportCsv.TabIndex = 1;
            btnImportCsv.Text = "Import";
            btnImportCsv.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1534, 1162);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(calendarControl);
            tabPage1.Location = new Point(4, 29);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1526, 1129);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // calendarControl
            // 
            calendarControl.BackColor = Color.White;
            calendarControl.CausesValidation = false;
            calendarControl.Dock = DockStyle.Fill;
            calendarControl.Location = new Point(3, 3);
            calendarControl.Name = "calendarControl";
            calendarControl.Size = new Size(1520, 1123);
            calendarControl.TabIndex = 0;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(dataGridView1);
            tabPage2.Location = new Point(4, 29);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1184, 1006);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(99, 135);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.Size = new Size(300, 188);
            dataGridView1.TabIndex = 0;
            // 
            // AccountPage
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Name = "AccountPage";
            Size = new Size(2296, 1162);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridAutoPays).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private DataGridView gridAutoPays;
        private Controls.Charts.MiniBalanceLineChart miniBalanceLineChart1;
        private Button btnImportQuicken;
        private Panel panel1;
        private Button btnDeleteTransaction;
        private TextBox txtAmount;
        private TextBox txtTitle;
        private Button btnAddTransaction;
        private ComboBox cboAccounts;
        private DateTimePicker dtTransactionDate;
        private Button btnImportCsv;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private Controls.Calendar.CalendarView calendarControl;
        private TabPage tabPage2;
        private DataGridView dataGridView1;
    }
}
