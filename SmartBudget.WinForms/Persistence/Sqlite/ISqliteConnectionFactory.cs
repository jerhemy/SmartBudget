using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SmartBudget.Infrastructure.Persistence.Sqlite
{
    public interface ISqliteConnectionFactory
    {
        IDbConnection CreateOpenConnection();
    }
}
