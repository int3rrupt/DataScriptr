//using DatabaseDevelopment;
//using DatabaseDevelopment.Models;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System.Collections.Generic;
//using System.Text.RegularExpressions;

//namespace DatabaseDevelopmentTests
//{
//    [TestClass()]
//    public class SqlStatementParserTests
//    {
//        #region Single Select Statement

//        private const string SingleSelectQueryFromClauseSingleLine = @"
//                select top 100 * from [Northwind].[dbo].[customers] inner join someOtherTable
//                on Field = Field
//                where Field = Value
//                ";
//        private const string SingleSelectQueryFromClauseSingleLine_SelectText = " select  top 100 *  from [Database].[Schema].[Table] inner  join [Database].[Schema].[Table] on [Database].[Schema].[Table].[Field] = [Database].[Schema].[Table].[Field] where [Database].[Schema].[Table].[Field] = 'Value'";
//        private const string SingleSelectQueryFromClauseSingleLine_FromText = " from [Database].[Schema].[Table] inner  join [Database].[Schema].[Table] on [Database].[Schema].[Table].[Field] = [Database].[Schema].[Table].[Field]";
//        private const string SingleSelectQueryFromClauseSingleLine_PrimaryTableDatabase = "Database";
//        private const string SingleSelectQueryFromClauseSingleLine_PrimaryTableSchema = "Schema";
//        private const string SingleSelectQueryFromClauseSingleLine_PrimaryTableName = "Table";
//        private const string SingleSelectQueryFromClauseSingleLine_PrimaryTableFullName = "Database.Schema.Table";

//        [TestMethod()]
//        public void ParseQuery_SingleSelectQueryFromClauseSingleLine_StatementCountTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(SingleSelectQueryFromClauseSingleLine);
//            Assert.IsTrue(selectStatements.Count == 1);
//        }
//        [TestMethod()]
//        public void ParseQuery_SingleSelectQueryFromClauseSingleLine_ErrorMessageTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(SingleSelectQueryFromClauseSingleLine);
//            Assert.IsTrue(selectStatements[0].ErrorMessages.Count == 0);
//        }
//        [TestMethod()]
//        public void ParseQuery_SingleSelectQueryFromClauseSingleLine_SelectTextTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(SingleSelectQueryFromClauseSingleLine);
//            Assert.IsTrue(selectStatements[0].SelectText == SingleSelectQueryFromClauseSingleLine_SelectText);
//        }
//        [TestMethod()]
//        public void ParseQuery_SingleSelectQueryFromClauseSingleLine_FromTextTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(SingleSelectQueryFromClauseSingleLine);
//            Assert.IsTrue(selectStatements[0].FromText == SingleSelectQueryFromClauseSingleLine_FromText);
//        }
//        [TestMethod()]
//        public void ParseQuery_SingleSelectQueryFromClauseSingleLine_TableDatabaseTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(SingleSelectQueryFromClauseSingleLine);
//            Assert.IsTrue(selectStatements[0].PrimaryTableDatabase == SingleSelectQueryFromClauseSingleLine_PrimaryTableDatabase);
//        }
//        [TestMethod()]
//        public void ParseQuery_SingleSelectQueryFromClauseSingleLine_TableSchemaTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(SingleSelectQueryFromClauseSingleLine);
//            Assert.IsTrue(selectStatements[0].PrimaryTableName.Schema == SingleSelectQueryFromClauseSingleLine_PrimaryTableSchema);
//        }
//        [TestMethod()]
//        public void ParseQuery_SingleSelectQueryFromClauseSingleLine_TableNameTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(SingleSelectQueryFromClauseSingleLine);
//            Assert.IsTrue(selectStatements[0].PrimaryTableName.Name == SingleSelectQueryFromClauseSingleLine_PrimaryTableName);
//        }
//        [TestMethod()]
//        public void ParseQuery_SingleSelectQueryFromClauseSingleLine_TableFullNameTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(SingleSelectQueryFromClauseSingleLine);
//            Assert.IsTrue(selectStatements[0].PrimaryTableName.FullName == SingleSelectQueryFromClauseSingleLine_PrimaryTableFullName);
//        }

//        #endregion

//        #region Double Select Statement

//        private const string DoubleSelectQueryFromClauseMixedLine = @"
//                select top 100 * from [Database].[Schema].[Table] inner join [Database].[Schema].[Table]
//                on[Database].[Schema].[Table].[Field] = [Database].[Schema].[Table].[Field]
//                where[Database].[Schema].[Table].[Field] = 'Value'

//                select top 100
//                    *
//                from
//                    [Database].[Schema].[Table]
//                where
//                    Field > 1235435345
//                ";
//        // First select statement
//        private const string DoubleSelectQueryFromClauseMixedLine_SelectText1 = " select  top 100 *  from [Database].[Schema].[Table] inner  join [Database].[Schema].[Table] on [Database].[Schema].[Table].[Field] = [Database].[Schema].[Table].[Field] where [Database].[Schema].[Table].[Field] = 'Value'";
//        private const string DoubleSelectQueryFromClauseMixedLine_FromText1 = " from [Database].[Schema].[Table] inner  join [Database].[Schema].[Table] on [Database].[Schema].[Table].[Field] = [Database].[Schema].[Table].[Field]";
//        private const string DoubleSelectQueryFromClauseMixedLine_PrimaryTableDatabase1 = "Database";
//        private const string DoubleSelectQueryFromClauseMixedLine_PrimaryTableSchema1 = "Schema";
//        private const string DoubleSelectQueryFromClauseMixedLine_PrimaryTableName1 = "Table";
//        private const string DoubleSelectQueryFromClauseMixedLine_PrimaryTableFullName1 = "Database.Schema.Table";
//        // Second select statement
//        private const string DoubleSelectQueryFromClauseMixedLine_SelectText2 = " select  top 100 *  from [Database].[Schema].[Table] where Field > 234234";
//        private const string DoubleSelectQueryFromClauseMixedLine_FromText2 = " from [Database].[Schema].[Table]";
//        private const string DoubleSelectQueryFromClauseMixedLine_PrimaryTableDatabase2 = "Database";
//        private const string DoubleSelectQueryFromClauseMixedLine_PrimaryTableSchema2 = "Schema";
//        private const string DoubleSelectQueryFromClauseMixedLine_PrimaryTableName2 = "Table";
//        private const string DoubleSelectQueryFromClauseMixedLine_PrimaryTableFullName2 = "Database.Schema.Table";

//        [TestMethod()]
//        public void ParseQuery_DoubleSelectQueryFromClauseMixedLine_StatementCountTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(DoubleSelectQueryFromClauseMixedLine);
//            Assert.IsTrue(selectStatements.Count == 2);
//        }
//        [TestMethod()]
//        public void ParseQuery_DoubleSelectQueryFromClauseMixedLine_ErrorMessageTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(DoubleSelectQueryFromClauseMixedLine);
//            Assert.IsTrue(selectStatements[0].ErrorMessages.Count == 0);
//        }
//        [TestMethod()]
//        public void ParseQuery_DoubleSelectQueryFromClauseMixedLine_SelectTextTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(DoubleSelectQueryFromClauseMixedLine);
//            Assert.IsTrue(selectStatements[0].SelectText == DoubleSelectQueryFromClauseMixedLine_SelectText1);
//            Assert.IsTrue(selectStatements[1].SelectText == DoubleSelectQueryFromClauseMixedLine_SelectText2);
//        }
//        [TestMethod()]
//        public void ParseQuery_DoubleSelectQueryFromClauseMixedLine_FromTextTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(DoubleSelectQueryFromClauseMixedLine);
//            Assert.IsTrue(
//                selectStatements[0].FromText == DoubleSelectQueryFromClauseMixedLine_FromText1,
//                $"From text does not match first select statement. Actual value: {selectStatements[0].FromText}");
//            Assert.IsTrue(selectStatements[1].FromText == DoubleSelectQueryFromClauseMixedLine_FromText2,
//                $"From text does not match second select statement. Actual value: {selectStatements[1].FromText}");
//        }
//        [TestMethod()]
//        public void ParseQuery_DoubleSelectQueryFromClauseMixedLine_TableDatabaseTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(DoubleSelectQueryFromClauseMixedLine);
//            Assert.IsTrue(selectStatements[0].PrimaryTableDatabase == DoubleSelectQueryFromClauseMixedLine_PrimaryTableDatabase1);
//            Assert.IsTrue(selectStatements[1].PrimaryTableDatabase == DoubleSelectQueryFromClauseMixedLine_PrimaryTableDatabase2);
//        }
//        [TestMethod()]
//        public void ParseQuery_DoubleSelectQueryFromClauseMixedLine_TableSchemaTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(DoubleSelectQueryFromClauseMixedLine);
//            Assert.IsTrue(selectStatements[0].PrimaryTableName.Schema == DoubleSelectQueryFromClauseMixedLine_PrimaryTableSchema1);
//            Assert.IsTrue(selectStatements[1].PrimaryTableName.Schema == DoubleSelectQueryFromClauseMixedLine_PrimaryTableSchema2);
//        }
//        [TestMethod()]
//        public void ParseQuery_DoubleSelectQueryFromClauseMixedLine_TableNameTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(DoubleSelectQueryFromClauseMixedLine);
//            Assert.IsTrue(selectStatements[0].PrimaryTableName.Name == DoubleSelectQueryFromClauseMixedLine_PrimaryTableName1);
//            Assert.IsTrue(selectStatements[1].PrimaryTableName.Name == DoubleSelectQueryFromClauseMixedLine_PrimaryTableName2);
//        }
//        [TestMethod()]
//        public void ParseQuery_DoubleSelectQueryFromClauseMixedLine_TableFullNameTest()
//        {
//            List<SelectStatement> selectStatements = SqlStatementParser.ParseSelectStatement(DoubleSelectQueryFromClauseMixedLine);
//            Assert.IsTrue(selectStatements[0].PrimaryTableName.FullName == DoubleSelectQueryFromClauseMixedLine_PrimaryTableFullName1);
//            Assert.IsTrue(selectStatements[1].PrimaryTableName.FullName == DoubleSelectQueryFromClauseMixedLine_PrimaryTableFullName2);
//        }

//        #endregion

//        [TestMethod()]
//        public void Debug()
//        {

//            string query = @"
//                    select top 100 * from[Database].[Schema].[Table] inner join[Database].[Schema].[Table]
//                    on[Database].[Schema].[Table].[Field] = [Database].[Schema].[Table].[Field]
//                    where[Database].[Schema].[Table].[Field] = 'Value'
//                    ";


//            string scriptsDir = @"C:\Users\user\Desktop\localdb\Database\_Data";

//            string server = "someServer";
//            string database = "database";
//            string configServer = "configServer";
//            DataExporter dataExporter = new DataExporter(server, database, configServer, "localdatabase");
//            dataExporter.ExportDataToScripts(query, scriptsDir, DatabaseDevelopment.Enums.DataScriptType.Test, DatabaseDevelopment.Enums.SqlScriptType.Merge, true, true, true, "Test");
//        }

//        [TestMethod()]
//        public void Test()
//        {
//            while (true)
//            {
//                string input = @"
//(123,N'SomeValue',N'Stuff',N'OtherStuff')
// ,(124,N'Value',N'Stuff',N'Data')
//                ";
//                //(?:^\s*,?\(((?:[^)]+)|(?:(.*)))\)$)
//                Regex reg = new Regex(@"(?:^\s*,?\(((?:.|\r|\n|\r\n)*?)\)$)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
//                var matches = reg.Matches(input);
//                string pattern = @"(?:^\s*,?\(((?:.|\r|\n|\r\n)*?)\)$)";
//                var matchCollection = Regex.Matches(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
//                foreach (Match match in matchCollection)
//                {
//                    System.Console.WriteLine(match.Value);
//                }
//            }
//        }
//    }
//}