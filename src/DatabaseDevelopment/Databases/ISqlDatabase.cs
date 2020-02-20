using System.Data;

namespace DatabaseDevelopment.Standard.Databases
{
    public interface ISqlDatabase
    {
        DataSet ExecuteQuery(string query);
        DataTable GetPrimaryKeyConstraintsFromDatabase();
        DataTable GetForeignKeyConstraintsTableFromDatabase(bool includeTableMaintenanceTables = false);
    }
}