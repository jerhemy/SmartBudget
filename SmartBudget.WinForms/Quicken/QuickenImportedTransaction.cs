using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBudget.WinForms.Quicken;
public sealed record QuickenImportedTransaction(DateOnly PostedDate, decimal Amount, string? FitId, string? Name, string? Memo, string? CheckNumber);