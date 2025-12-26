using SmartBudget.WinForms.Abstractions;
using SmartBudget.WinForms.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SmartBudget.WinForms.Navigation
{
    public sealed partial class ImportPage : UserControl, IPage
    {
        public PageKey Key => PageKey.Import;
        public string Title => "Import";
        public Control View => this;

        public ImportPage()
        {
            InitializeComponent();
        }

        public Task OnNavigatedTo(NavigationContext context)
        {
            //throw new NotImplementedException();
            return Task.CompletedTask;
        }
    }
}
