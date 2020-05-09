//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace DataScriptr.Library.Parsing
//{
//    public class RegexSqlParseHelper
//    {
//        public string GetColumnMatches(string tableCreateColumns)
//        {
//            string columnDefinitionsReplaceNewLine = tableCreateColumns.Replace("\r\n", "\n");
//            // Get individual column definitions
//            regexPatt2ern = @"(?im)(?:(?<TableConstraintDefinition>^\s*(?:constraint\s+\[?(?<TableConstraintName>[^\s\]]+)\]?\s+)?(?:(?<TableConstraintUnique>(?<TableConstraintUniqueType>primary\s+key|unique)\s+(?:clustered|non\s*clustered)\s*(?:\((?<TableConstraintColumnNames>[^\)]+)\))?)|(?<TableConstraintForeignKey>foreign[\s]+key[\s]+[\S]+[\s]+references[\s]+[\S]+[\s]+\([^\)]+\))),?)|(?<ColumnDefinition>^\s*\[?(?<ColumnName>(?!constraint)[^\s,\]]+)\]?[\s]+(?:(?:\[?(?<ColumnDataType>[a-z2_]+)\]?)|(?:\[?(?<CustomColumnDataTypeSchema>[a-z2_]+)\]?.\[?(?<CustomColumnDataType>[a-z2_]+)\]?))[\s]*(?:\((?<ColumnDataTypePrecision>(?:max|[0-9]+|[0-9]+,[\s]+[0-9]))\))?[\s]+(?<ColumnConstraint>(?:constraint[\s]+[\S]+[\s]+(?:default[\s]+[\S]+)?))?[\s]*(?<ColumnIdentity>identity[\s]*(?:\([0-9]+,[\s]*[0-9]+\))?)?[\s]*(?<ColumnNotForReplication>not[\s]+for[\s]+replication)?[\s]*(?<ColumnRowGuidCol>rowguidcol\s*)?(?<ColumnNullability>null|not[\s]null)?,?\s*$(?<TableConstraint>primary[\s]key|unique)?,?))";
//            MatchCollection columnDefinitionRegexMatch = Regex.Matches(columnDefinitionsReplaceNewLine, regexPattern);
//            if (columnDefinitionRegexMatch.Count < 0)
//            {
//                throw new SqlScriptParsingException($"Unable to parse table create statement - individual column definitions. Regex: {regexPattern}", columnDefinitionsReplaceNewLine);
//            }
//        }
//    }
//}