
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EStore.Services.Database;

// This class is to inject configuration into the database connection.  For SQLite specifically, it enables the vector database extension.
public class ConnectionIntercepter : DbConnectionInterceptor
{
    public override DbConnection ConnectionCreated(ConnectionCreatedEventData eventData, DbConnection result)
    {
        if (result is SqliteConnection sqliteConnection)
        {
            sqliteConnection.EnableExtensions(true);
            sqliteConnection.LoadExtension("vec0");
        }

        return result;
    }
}