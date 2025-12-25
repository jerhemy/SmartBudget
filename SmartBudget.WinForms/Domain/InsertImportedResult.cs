using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBudget.Domain
{
    public sealed record InsertImportedResult(int Inserted, int SkippedAsDuplicate);
}
