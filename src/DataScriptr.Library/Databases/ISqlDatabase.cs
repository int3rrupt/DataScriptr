using System.Data;

namespace DataScriptr.Library.Databases
{
    public interface ISqlDatabase
    {
        DataSet ExecuteQuery(string query);
        DataTable GetPrimaryKeyConstraintsFromDatabase();
        DataTable GetForeignKeyConstraintsTableFromDatabase(bool includeTableMaintenanceTables = false);
    }
}