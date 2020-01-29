using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using Dapper;
using System.Data;

namespace Volyar
{
    public interface IDapperConnection
    {
        DbConnection NewConnection();
    }

    public class DapperConnection : IDapperConnection
    {
        private readonly string connectionType;
        private readonly string connectionString;

        public DapperConnection(string connectionType, string connectionString)
        {
            this.connectionType = connectionType;
            this.connectionString = connectionString;
        }

        public DbConnection NewConnection()
        {
            switch (connectionType)
            {
                case "temp":
                case "sqlite":
                    return new SqliteConnection(connectionString);
                case "sqlserver":
                    return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            }
            return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        }
    }

    public class TimeSpanHandler : SqlMapper.TypeHandler<TimeSpan>
    {
        public override TimeSpan Parse(object value)
        {
            return TimeSpan.Parse((string)value);
        }

        public override void SetValue(IDbDataParameter parameter, TimeSpan value)
        {
            parameter.Value = value.ToString();
        }
    }

    public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value)
        {
            return DateTimeOffset.Parse((string)value);
        }

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            parameter.Value = value.ToString();
        }
    }
}
