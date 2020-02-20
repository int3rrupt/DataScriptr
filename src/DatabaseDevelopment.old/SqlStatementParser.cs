using DatabaseDevelopment.Enums;
using DatabaseDevelopment.Exceptions;
using DatabaseDevelopment.Models;
using DatabaseDevelopment.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using TSQL;
using TSQL.Statements;
using TSQL.Tokens;

namespace DatabaseDevelopment
{
    public static class SqlStatementParser
    {
        #region Private Fields

        public static Dictionary<string, SqlDbType> sqlDbEngineTypeToSqlDbType;
        public static Dictionary<SqlDbType, Type> sqlDbTypeToNativeType;

        #endregion

        #region Static Constructor

        static SqlStatementParser()
        {
            sqlDbEngineTypeToSqlDbType = new Dictionary<string, SqlDbType>
            {
                { "bigint", SqlDbType.BigInt},
                { "binary", SqlDbType.VarBinary},
                { "bit", SqlDbType.Bit},
                { "char", SqlDbType.Char},
                { "date", SqlDbType.Date},
                { "datetime", SqlDbType.DateTime},
                { "datetime2", SqlDbType.DateTime2},
                { "datetimeoffset", SqlDbType.DateTimeOffset},
                { "decimal", SqlDbType.Decimal},
                { "varbinary(max)", SqlDbType.VarBinary},
                { "float", SqlDbType.Float},
                { "image", SqlDbType.Binary},
                { "int", SqlDbType.Int},
                { "money", SqlDbType.Money},
                { "nchar", SqlDbType.NChar},
                { "ntext", SqlDbType.NText},
                { "numeric", SqlDbType.Decimal},
                { "nvarchar", SqlDbType.NVarChar},
                { "real", SqlDbType.Real},
                { "rowversion", SqlDbType.Timestamp},
                { "smalldatetime", SqlDbType.DateTime},
                { "smallint", SqlDbType.SmallInt},
                { "smallmoney", SqlDbType.SmallMoney},
                { "sql_variant", SqlDbType.Variant},
                { "text", SqlDbType.Text},
                { "time", SqlDbType.Time},
                { "timestamp", SqlDbType.Timestamp},
                { "tinyint", SqlDbType.TinyInt},
                { "uniqueidentifier", SqlDbType.UniqueIdentifier},
                { "varbinary", SqlDbType.VarBinary},
                { "varchar", SqlDbType.VarChar},
                { "xml", SqlDbType.Xml}
            };
            sqlDbTypeToNativeType = new Dictionary<SqlDbType, Type>
            {
                { SqlDbType.BigInt, typeof(long) },
                { SqlDbType.Bit, typeof(bool) },
                { SqlDbType.Char, typeof(string) },
                { SqlDbType.Date, typeof(DateTime) },
                { SqlDbType.DateTime, typeof(DateTime) },
                { SqlDbType.DateTime2, typeof(DateTime) },
                { SqlDbType.DateTimeOffset, typeof(DateTimeOffset) },
                { SqlDbType.Decimal, typeof(decimal) },
                { SqlDbType.Float, typeof(double) },
                { SqlDbType.Image, typeof(byte[]) },
                { SqlDbType.Int, typeof(int) },
                { SqlDbType.Money, typeof(decimal) },
                { SqlDbType.NChar, typeof(string) },
                { SqlDbType.NText, typeof(string) },
                { SqlDbType.NVarChar, typeof(string) },
                { SqlDbType.Real, typeof(Single) },
                { SqlDbType.SmallInt, typeof(short) },
                { SqlDbType.SmallMoney, typeof(decimal) },
                { SqlDbType.Variant, typeof(object) },
                { SqlDbType.Text, typeof(string) },
                { SqlDbType.Time, typeof(TimeSpan) },
                { SqlDbType.Timestamp, typeof(byte[]) },
                { SqlDbType.TinyInt, typeof(byte) },
                { SqlDbType.UniqueIdentifier, typeof(Guid) },
                { SqlDbType.VarBinary, typeof(byte[]) },
                { SqlDbType.VarChar, typeof(string) },
                { SqlDbType.Xml, typeof(XmlDocument) }
            };
        }

        #endregion

        #region Parse Methods

        public static List<SelectStatement> ParseSelectStatement(string query)
        {
            // Parse query
            List<TSQLStatement> tSQLStatementList = TSQLStatementReader.ParseStatements(query);

            List<SelectStatement> selectStatements = new List<SelectStatement>();
            // Iterate through select statements
            foreach (TSQLStatement tSQLStatement in tSQLStatementList)
            {
                if (tSQLStatement is TSQLSelectStatement tSQLSelectStatement)
                {
                    List<string> errorMessages = new List<string>();
                    // Get select statements as text
                    string selectText = JoinTSqlStatementTokens(tSQLSelectStatement.Tokens);
                    // Get FROM clause as text
                    string fromText = JoinTSqlStatementTokens(tSQLSelectStatement.From.Tokens);
                    // Get table name in FROM clause
                    Match tableNameRegexMatch = Regex.Match(fromText, @"(?i)(?:[\s]*)from(?:[\s]+)(?:\[?(?<FromCapture1>[^\[\]. ]*)\]?.)(?:\[?(?<FromCapture2>[^\[\]. ]*)\]?(?:.\[?(?<FromCapture3>[^\[\]. ]*)\]?)?)");
                    if (tableNameRegexMatch.Success)
                    {
                        // Determine if database name included 
                        Group matchSearch = tableNameRegexMatch.Groups["FromCapture3"];
                        bool databaseNameIncluded = matchSearch.Success && !string.IsNullOrWhiteSpace(matchSearch.Value);
                        // Get database or table schema
                        string capture1 = string.Empty;
                        matchSearch = tableNameRegexMatch.Groups["FromCapture1"];
                        if (matchSearch.Success)
                        {
                            capture1 = matchSearch.Value;
                        }
                        else
                        {
                            string type = databaseNameIncluded ? "Database Name" : "Table Schema";
                            errorMessages.Add($"Could not determine {type} in FROM clause");
                        }
                        // Get table schema or table name
                        string capture2 = string.Empty;
                        matchSearch = tableNameRegexMatch.Groups["FromCapture2"];
                        if (matchSearch.Success)
                        {
                            capture2 = matchSearch.Value;
                        }
                        else
                        {
                            string type = databaseNameIncluded ? "Table Schema" : "Table Name";
                            errorMessages.Add($"Could not determine {type} in FROM clause");
                        }
                        // Get table name
                        string capture3 = string.Empty;
                        matchSearch = tableNameRegexMatch.Groups["FromCapture3"];
                        if (matchSearch.Success)
                        {
                            capture3 = matchSearch.Value;
                        }
                        //else
                        //{
                        //    throw new Exception("Could not determine Table Name in FROM clause");
                        //}
                        // Set values
                        string databaseName = string.Empty;
                        string tableSchema = string.Empty;
                        string tableName = string.Empty;
                        if (databaseNameIncluded)
                        {
                            databaseName = capture1;
                            tableSchema = capture2;
                            tableName = capture3;
                        }
                        else
                        {
                            tableSchema = capture1;
                            tableName = capture2;
                        }
                        // Update Table Name
                        selectStatements.Add(new SelectStatement(selectText, fromText, databaseName, tableSchema, tableName, tSQLSelectStatement, errorMessages));
                    }
                }
            }

            return selectStatements;
        }

        public static DataTable ParseTableCreate(string statement)
        {
            try
            {
                List<string> warningMessages = new List<string>();
                // Get first create statement
                Match createRegexMatch = Regex.Match(statement, @"(?i)create[^;]+;");
                if (!createRegexMatch.Success)
                {
                    throw new Exception("Unable to parse create statement.");
                }
                // Get create statement parts
                Match tableCreateRegexMatch = Regex.Match(createRegexMatch.Value, @"(?i)create[\s]+table[\s]+(?<TableSchema>[^.]+).(?<TableName>[^\s]+)\s*\((?<ColumnDefinitions>(?:.|\s)+)\);");
                if (!tableCreateRegexMatch.Success)
                {
                    throw new SqlScriptParsingException("Unable to parse table create statement.");
                }
                // Get table schema
                Group matchSearch = tableCreateRegexMatch.Groups["TableSchema"];
                if (!matchSearch.Success)
                {
                    throw new SqlScriptParsingException("Unable to parse table create statement - TableSchema.");
                }
                string tableSchema = matchSearch.Value;
                tableSchema = tableSchema.Replace("[", "").Replace("]", "");
                // Get table name
                matchSearch = tableCreateRegexMatch.Groups["TableName"];
                if (!matchSearch.Success)
                {
                    throw new SqlScriptParsingException("Unable to parse table create statement - TableName.");
                }
                string tableName = matchSearch.Value;
                tableName = tableName.Replace("[", "").Replace("]", "");
                // Get field definitions
                matchSearch = tableCreateRegexMatch.Groups["ColumnDefinitions"];
                if (!matchSearch.Success)
                {
                    throw new SqlScriptParsingException("Unable to parse table create statement - ColumnDefinitions.");
                }
                string columnDefinitionsReplaceNewLine = matchSearch.Value.Replace("\r\n", "\n");
                // Get individual column definitions
                //MatchCollection columnDefinitionRegexMatch = Regex.Matches(matchSearch.Value, @"(?i)(?:(?<ColumnDefinition>[\s]*(?<ColumnName>[\S]+)[\s]+(?<DataType>[a-z2_]+)[\s]*(?<Precision>\((?:max|[0-9]+|[0-9]+,[\s]+[0-9])\))?[\s]+(?<Nullability>null|not[\s]null)?,?(?<ColumnConstraint>primary[\s]key|unique)?,?)|(?<ColumnConstraintDefinition>[\s]*constraint[\s]+[\S]+[\s]+(?:(?:(?:primary[\s]+key|unique|)[\s]+(?:clustered|non[\s]clustered)[\s]*(?:\([^\)]+\))?)|(?:foreign[\s]+key[\s]+[\S]+[\s]+references[\s]+[\S]+[\s]+\([^\)]+\))),?))");
                //MatchCollection columnDefinitionRegexMatch = Regex.Matches(matchSearch.Value, @"(?i)(?:(?<ColumnDefinition>[\s]*(?<ColumnName>(?!constraint)[\S]+)[\s]+(?<DataType>[\[a-z2_\]]+)[\s]*(?<Precision>\((?:max|[0-9]+|[0-9]+,[\s]+[0-9])\))?[\s]+(?<Nullability>null|not[\s]null)?,?(?<ColumnConstraint>primary[\s]key|unique)?,?)|(?<ColumnConstraintDefinition>[\s]*constraint[\s]+(?<ConstraintName>[\S]+)[\s]+(?:(?:(?:(?<ConstraintType>primary[\s]+key)|unique|)[\s]+(?:clustered|non[\s]clustered)[\s]*(?:\((?<ConstraintColumnNames>[^\)]+)\))?)|(?:foreign[\s]+key[\s]+[\S]+[\s]+references[\s]+[\S]+[\s]+\([^\)]+\))),?))");
                //MatchCollection columnDefinitionRegexMatch = Regex.Matches(matchSearch.Value, @"(?i)(?:(?<ColumnDefinition>\[?(?<ColumnName>(?!constraint)[^\s,\]]+)\]?[\s]+(?:\[?(?<ColumnDataType>[a-z2_]+)\]?)[\s]*(?:\((?<ColumnDataTypePrecision>(?:max|[0-9]+|[0-9]+,[\s]+[0-9]))\))?[\s]+(?<ColumnConstraint>(?:constraint[\s]+[\S]+[\s]+(?:default[\s]+[\S]+)?))?[\s]*(?<ColumnIdentity>identity[\s]*(?:\([0-9]+,[\s]*[0-9]+\))?)?[\s]*(?<ColumnNotForReplication>not[\s]+for[\s]+replication)?[\s]*(?<ColumnNullability>null|not[\s]null)?,?(?<TableConstraint>primary[\s]key|unique)?,?)|(?<TableConstraintDefinition>constraint[\s]+\[?(?<TableConstraintName>[^\s\]]+)\]?[\s]+(?:(?<TableConstraintUnique>(?<TableConstraintUniqueType>primary[\s]+key|unique)[\s]+(?:clustered|non[\s]clustered)[\s]*(?:\((?<TableConstraintColumnNames>[^\)]+)\))?)|(?<TableConstraintForeignKey>foreign[\s]+key[\s]+[\S]+[\s]+references[\s]+[\S]+[\s]+\([^\)]+\))),?))");
                MatchCollection columnDefinitionRegexMatch = Regex.Matches(columnDefinitionsReplaceNewLine, @"(?im)(?:(?<TableConstraintDefinition>^\s*(?:constraint\s+\[?(?<TableConstraintName>[^\s\]]+)\]?\s+)?(?:(?<TableConstraintUnique>(?<TableConstraintUniqueType>primary\s+key|unique)\s+(?:clustered|non\s*clustered)\s*(?:\((?<TableConstraintColumnNames>[^\)]+)\))?)|(?<TableConstraintForeignKey>foreign[\s]+key[\s]+[\S]+[\s]+references[\s]+[\S]+[\s]+\([^\)]+\))),?)|(?<ColumnDefinition>^\s*\[?(?<ColumnName>(?!constraint)[^\s,\]]+)\]?[\s]+(?:\[?(?<ColumnDataType>[a-z2_]+)\]?)[\s]*(?:\((?<ColumnDataTypePrecision>(?:max|[0-9]+|[0-9]+,[\s]+[0-9]))\))?[\s]+(?<ColumnConstraint>(?:constraint[\s]+[\S]+[\s]+(?:default[\s]+[\S]+)?))?[\s]*(?<ColumnIdentity>identity[\s]*(?:\([0-9]+,[\s]*[0-9]+\))?)?[\s]*(?<ColumnNotForReplication>not[\s]+for[\s]+replication)?[\s]*(?<ColumnNullability>null|not[\s]null)?,?\s*$(?<TableConstraint>primary[\s]key|unique)?,?))");
                if (columnDefinitionRegexMatch.Count < 0)
                {
                    throw new SqlScriptParsingException("Unable to parse table create statement - individual column definitions.");
                }
                DataTable dataTable = new DataTable($"{tableSchema}.{tableName}");
                DataColumn dataColumn;
                // Iterate through individual column definitions and create new data column objects
                foreach (Match definition in columnDefinitionRegexMatch)
                {
                    // Check whether definition is table constraint
                    if (definition.Groups["TableConstraintDefinition"].Success)
                    {
                        // Check if primary key
                        matchSearch = definition.Groups["TableConstraintUniqueType"];
                        if (matchSearch.Success && matchSearch.Value.ToLower().Contains("primary"))
                        {
                            matchSearch = definition.Groups["TableConstraintColumnNames"];
                            if (matchSearch.Success)
                            {
                                List<DataColumn> primaryKeyColumns = new List<DataColumn>();
                                MatchCollection primaryKeyColumnNameMatchCollection = Regex.Matches(matchSearch.Value, @"(?i)\[?(?<PrimaryKeyColumnName>[^\s\]]+)\]?[\s]+(?:asc|desc)");
                                foreach (Match primaryKeyColumnNameMatch in primaryKeyColumnNameMatchCollection)
                                {
                                    // Get primary key column name
                                    matchSearch = primaryKeyColumnNameMatch.Groups["PrimaryKeyColumnName"];
                                    if (!matchSearch.Success)
                                    {
                                        throw new SqlScriptParsingException("Unable to parse table primary key - ColumnName.");
                                    }
                                    string primaryKeyColumnName = matchSearch.Value;
                                    primaryKeyColumns.Add(dataTable.Columns[primaryKeyColumnName]);
                                }
                                dataTable.PrimaryKey = primaryKeyColumns.ToArray();
                            }
                        }
                    }
                    else
                    {
                        // Get column name
                        matchSearch = definition.Groups["ColumnName"];
                        if (!matchSearch.Success)
                        {
                            throw new SqlScriptParsingException("Unable to parse table create statement - individual column definitions - ColumnName.");
                        }
                        string columnName = matchSearch.Value;
                        // Get data type
                        matchSearch = definition.Groups["ColumnDataType"];
                        if (!matchSearch.Success)
                        {
                            throw new SqlScriptParsingException("Unable to parse table create statement - individual column definitions - DataType.");
                        }
                        string dataType = matchSearch.Value;
                        // Get precision
                        string precision = string.Empty;
                        matchSearch = definition.Groups["ColumnDataTypePrecision"];
                        if (matchSearch.Success)
                        {
                            precision = matchSearch.Value;
                            precision = precision.Replace("(", "").Replace(")", "");
                        }
                        else
                        {
                            warningMessages.Add($"Column definition parsing warning - Could not determine Precision for: \"{definition.Value}\".");
                        }
                        // Get nullability
                        string nullability = string.Empty;
                        matchSearch = definition.Groups["ColumnNullability"];
                        if (matchSearch.Success)
                        {
                            nullability = matchSearch.Value;
                        }
                        else
                        {
                            warningMessages.Add($"Could not determine - Nullability.");
                        }
                        Type nativeDataType = SqlStatementParser.SqlDbEngineTypeToNativeType(dataType);
                        // Create new data column
                        dataColumn = new DataColumn(columnName, nativeDataType);
                        dataColumn.ExtendedProperties.Add("SqlDbEngineType", $"{dataType}{(string.IsNullOrWhiteSpace(precision) ? string.Empty : $"({precision})")}");
                        if (nativeDataType == typeof(string) && precision.ToLower() != "max")
                        {
                            dataColumn.MaxLength = int.Parse(precision);
                        }
                        dataColumn.AllowDBNull = string.IsNullOrWhiteSpace(nullability) || nullability.ToLower() == "null";
                        dataTable.Columns.Add(dataColumn);
                    }
                }
                return dataTable;
            }
            catch (SqlScriptParsingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SqlScriptParsingException($"Message: {ex.Message}\r\nStack: {ex.StackTrace}\r\nScript: {statement}");
            }
        }

        #endregion

        #region Public Helper Methods

        /// <summary>
        /// Joins all tokens into a string, adding spaces around Keyword and Operator token types.
        /// </summary>
        /// <param name="TokensList">A list of TSQLToken</param>
        /// <returns>A string of concatenated TSQLTokens</returns>
        public static string JoinTSqlStatementTokens(List<TSQLToken> tokens)
        {
            string joinedText = "";
            foreach (TSQLToken token in tokens)
            {
                switch (token.Type.ToString())
                {
                    case "Keyword":
                    case "Operator":
                        {
                            joinedText += $" {token.Text} ";
                            break;
                        }
                    default:
                        {
                            joinedText += token.Text;
                            break;
                        }
                }
            }

            return joinedText;
        }

        public static string NewSelectWhereKeysInValues(string TableFullNameDelimited, Dictionary<string, List<string>> keysAndValues)
        {
            string whereClause = NewWhereClauseFromKeys(keysAndValues);
            return $@"
                    SELECT
                        *
                    FROM
                        {TableFullNameDelimited}
                    WHERE
                        {whereClause}
                    ";
        }

        public static string NewWhereClauseFromKeys(Dictionary<string, List<string>> keysAndValues)
        {
            List<string> whereClauseList = new List<string>();
            foreach (string keyColumnName in keysAndValues.Keys)
            {
                string whereClause = $"[{keyColumnName}] in ('{string.Join("','", keysAndValues[keyColumnName].Select(key => key.ToString()))}')";
                whereClauseList.Add(whereClause);
            }
            return string.Join(" AND ", whereClauseList);
        }

        public static bool TrySqlDbEngineTypeToNativeType(string sqlDbEngineType, out Type nativeType)
        {
            try
            {
                nativeType = SqlStatementParser.SqlDbEngineTypeToNativeType(sqlDbEngineType);
                return true;
            }
            catch (Exception)
            {
                nativeType = null;
                return false;
            }
        }

        public static Type SqlDbEngineTypeToNativeType(string sqlDbEngineType)
        {
            if (!sqlDbEngineTypeToSqlDbType.TryGetValue(sqlDbEngineType.ToLower(), out SqlDbType sqlDbType))
            {
                throw new Exception($"SQL Database Engine Type '{sqlDbEngineType}' could not be converted to a native .NET type.");
            }
            return sqlDbTypeToNativeType[sqlDbType];
        }

        public static object SqlDbEngineValueToNativeValue(Type nativeValueType, string sqlDbEngineValue)
        {
            if (nativeValueType == typeof(bool))
            {
                if (sqlDbEngineValue == "0")
                {
                    sqlDbEngineValue = "false";
                }
                if (sqlDbEngineValue == "1")
                {
                    sqlDbEngineValue = "true";
                }
            }
            if (sqlDbEngineValue.ToLower() == "null")
            {
                return DBNull.Value;
            }
            if (nativeValueType == typeof(DateTime) && sqlDbEngineValue.ToLower() == "getdate()")
            {
                return DateTime.MaxValue;
            }
            return TypeDescriptor.GetConverter(nativeValueType).ConvertFromInvariantString(sqlDbEngineValue);
        }

        #endregion

        public static MergeScriptParseResult ParseMergeScript(string mergeStatement, DataTable dataTable)
        {
            try
            {
                DataTable resultDataTable = dataTable.Copy();
                List<string> warningMessages = new List<string>();
                bool deleteWhenNotMatched = false;
                bool printChanges = false;
                if (!string.IsNullOrWhiteSpace(mergeStatement))
                {
                    string mergeStatementRegex = @"(?i)\s*merge\s+into\s+\[(?<TableSchema>[^\]]+)\]\.\[(?<TableName>[^\]]+)\]\s+as\s+\[target\]\s+using\s+\(\s*(?:(?:values\s+(?<TableRows>[\S\s]+))|(?<EmptyDataSet>select\s+[\S]+\s+from\s+\[[^\]]+\].\[[^\]]+\]\s+where\s+1\s+=\s+0\s*-- empty dataset))\s*\)\s*as\s+\[source\]\s+\((?<SourceColumnNames>[\S]+)\)\s+on\s+\((?:\[(?:target|source)\]\.\[[^\]]+\]\s+=\s+\[(?:target|source)\]\.\[[^\]]+\](?:\s+and\s+)?)+\)\s+(?<WhenMatched>when\s+matched\s+and\s*\(\s+(?:nullif\(\[(?:target|source)\]\.\[[^\]]+\],\s+\[(?:target|source)\]\.\[[^\]]+\](?:\s+collate sql_latin1_general_cp1_cs_as\s+)?\)\s+is\s+not\s+null(?:\s+or\s+)?)+\)\s+then\s+update\s+set\s+(?:\[target\].\[[^\]]+\]\s+=\s+\[source\].\[[^\]]+\](?:,\s+)?)+)\s*(?<WhenNotMatchedByTarget>when\s+not\s+matched\s+by\s+target\s+then\s+insert\((?:\[[^\]]+\],?)+\)\s+values\((?:\[source\].\[[^\]]+\],?)+\))\s*(?<WhenNotMatchedBySource>when\s+not\s+matched\s+by\s+source\s+then\s+delete)?\s*(?<Output>output\s+[^;]+)?;";
                    // Get merge statement
                    Match mergeRegexMatch = Regex.Match(mergeStatement, mergeStatementRegex);
                    if (!mergeRegexMatch.Success)
                    {
                        throw new SqlScriptParsingException($"Unable to parse merge statement.{Environment.NewLine}Regex Used: {mergeStatementRegex}");
                    }
                    // Get table schema
                    Group matchSearch = mergeRegexMatch.Groups["TableSchema"];
                    if (!matchSearch.Success)
                    {
                        throw new SqlScriptParsingException("Unable to parse merge statement - TableSchema.");
                    }
                    string tableSchema = matchSearch.Value;
                    // Get table name
                    matchSearch = mergeRegexMatch.Groups["TableName"];
                    if (!matchSearch.Success)
                    {
                        throw new SqlScriptParsingException("Unable to parse merge statement - TableName.");
                    }
                    string tableName = matchSearch.Value;
                    // Get column names
                    matchSearch = mergeRegexMatch.Groups["SourceColumnNames"];
                    if (!matchSearch.Success)
                    {
                        throw new SqlScriptParsingException("Unable to parse merge statement - ColumnNames.");
                    }
                    // Create array of column names
                    MatchCollection regexMatches = Regex.Matches(matchSearch.Value, @"\[(?<ColumnName>[^\]\[]+)\]");
                    string[] columnNames = new string[regexMatches.Count];
                    for (int i = 0; i < regexMatches.Count; i++)
                    {
                        if (regexMatches[i].Success)
                        {
                            columnNames[i] = regexMatches[i].Groups["ColumnName"].Value;
                        }
                    }
                    matchSearch = mergeRegexMatch.Groups["EmptyDataSet"];
                    if (matchSearch.Success)
                    {
                        // No data to parse
                    }
                    else
                    {
                        // Get table rows
                        matchSearch = mergeRegexMatch.Groups["TableRows"];
                        if (!matchSearch.Success)
                        {
                            throw new SqlScriptParsingException("Unable to parse merge statement - TableRows.");
                        }
                        // Get individual rows
                        //MatchCollection tableRowsRegexMatch = Regex.Matches(matchSearch.Value, @"(?i)[\s]*\((.+)\),?");
                        string rowsNewLineReplaced = matchSearch.Value.Replace("\r\n", "\n");
                        MatchCollection tableRowsRegexMatch = Regex.Matches(rowsNewLineReplaced, @"(?:^\s*,?\(((?:.|\r|\n|\r\n)*?)\)\s*$)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        //int mode = 0;
                        RowParserStateContext rowParser = new RowParserStateContext();
                        foreach (Match tableRowMatch in tableRowsRegexMatch)
                        {
                            string rowValuesString = tableRowMatch.Groups[1].Value;
                            string[] columnValues = rowParser.ParseRow(rowValuesString, columnNames.Length);
                            DataRow dataRow = resultDataTable.NewRow();
                            for (int i = 0; i < columnNames.Length; i++)
                            {
                                //Console.WriteLine("Current ")
                                Type currentColumnType = resultDataTable.Columns[columnNames[i]].DataType;
                                var columnValue = SqlStatementParser.SqlDbEngineValueToNativeValue(currentColumnType, columnValues[i]);
                                dataRow[columnNames[i]] = columnValue;
                            }
                            resultDataTable.Rows.Add(dataRow);
                        }
                    }
                    resultDataTable.AcceptChanges();
                    // Get not matched by source clause
                    deleteWhenNotMatched = mergeRegexMatch.Groups["WhenNotMatchedBySource"].Success;
                    // Get output clause
                    printChanges = mergeRegexMatch.Groups["Output"].Success;
                }

                return new MergeScriptParseResult(
                    dataTable: resultDataTable,
                    deleteWhenNotMatched: deleteWhenNotMatched,
                    printChanges: printChanges,
                    warnings: warningMessages);
            }
            catch (SqlScriptParsingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SqlScriptParsingException($"Message: {ex.Message}\r\nStack: {ex.StackTrace}\r\nScript: {mergeStatement}");
            }
        }
    }
    public class MergeScriptParseResult
    {
        public DataTable DataTable { get; private set; }
        public bool DeleteWhenNotMatched { get; private set; }
        public bool PrintChanges { get; private set; }
        public ReadOnlyCollection<string> Warnings { get; private set; }
        public MergeScriptParseResult(DataTable dataTable, bool deleteWhenNotMatched, bool printChanges, List<string> warnings)
        {
            this.DataTable = dataTable;
            this.DeleteWhenNotMatched = deleteWhenNotMatched;
            this.PrintChanges = printChanges;
            this.Warnings = warnings.AsReadOnly();
        }
    }
    public class MergeScriptParseCollection
    {
        public Dictionary<DatabaseEnvironment, MergeScriptParseResult> Items { get; private set; }
        public bool ContainsErrors { get; private set; }
        public MergeScriptParseCollection()
        {
            this.Items = new Dictionary<DatabaseEnvironment, MergeScriptParseResult>();
            this.ContainsErrors = false;
        }
        public MergeScriptParseCollection(Dictionary<DatabaseEnvironment, MergeScriptParseResult> items, bool containsErrors)
        {
            this.Items = items;
            this.ContainsErrors = containsErrors;
        }
    }
}