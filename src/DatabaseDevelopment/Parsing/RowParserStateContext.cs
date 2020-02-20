using System;
using System.Collections.Generic;

namespace DatabaseDevelopment.Parsing
{
    public class RowParserStateContext
    {
        private Func<RowStringInfo, bool> ParsingFunction { get; set; }

        public string[] ParseRow(string rowString, int columnCount)
        {
            RowStringInfo rowStringInfo = new RowStringInfo(rowString, columnCount);
            this.ParsingFunction = Start;
            while (!this.ParsingFunction(rowStringInfo)) ;
            return rowStringInfo.ColumnValues;
        }

        #region States

        private object _stateLock = new object();


        private bool Start(RowStringInfo rowStringInfo)
        {
            switch (rowStringInfo.GetCurrentCharacterAsLower())
            {
                // Unicode string value
                case 'n':
                    {
                        // Check whether n character is followed by a single quote
                        if (rowStringInfo.TryPreviewNextCharacter(out char nextCharacter) && nextCharacter == '\'')
                        {
                            this.ParsingFunction = this.QuotedValueCapture;
                            rowStringInfo.CurrentStringIndex += 2;
                        }
                        else
                        {
                            this.ParsingFunction = this.NonQuotedValueCapture;
                        }
                        break;
                    }
                // non-unicode string value
                case '\'':
                    {
                        this.ParsingFunction = this.QuotedValueCapture;
                        rowStringInfo.CurrentStringIndex++;
                        break;
                    }
                case ' ':
                    {
                        // Continue waiting for keyword or value
                        rowStringInfo.CurrentStringIndex++;
                        break;
                    }
                case '\t':
                    {
                        // Ignore control character and continue waiting for keyword or value
                        rowStringInfo.CurrentStringIndex++;
                        break;
                    }
                // non-quoted value
                default:
                    {
                        this.ParsingFunction = this.NonQuotedValueCapture;
                        break;
                    }
            }
            return false;
        }

        private bool UnicodeValueCapture(RowStringInfo rowStringInfo)
        {
            // Wait for quote to begin capturing column value
            if (rowStringInfo.GetCurrentCharacter() == '\'')
            {
                this.ParsingFunction = this.QuotedValueCapture;
                rowStringInfo.CurrentStringIndex++;
            }
            return false;
        }

        private bool QuotedValueCapture(RowStringInfo rowStringInfo)
        {
            switch (rowStringInfo.GetCurrentCharacterAsLower())
            {
                case '\'':
                    {
                        // Check whether current quote is followed by second quote, making it an escaped quote
                        if (rowStringInfo.TryPreviewNextCharacter(out char nextCharacter) && nextCharacter == '\'')
                        {
                            rowStringInfo.CurrentColumnValue += rowStringInfo.GetCurrentCharacter();
                            rowStringInfo.CurrentColumnValue += nextCharacter;
                            // Already saved second quote, skip
                            rowStringInfo.CurrentStringIndex += 2;
                        }
                        // Otherwise the end of the field value has been reached
                        else
                        {
                            // If empty string
                            if (rowStringInfo.CurrentColumnValue == null)
                            {
                                rowStringInfo.CurrentColumnValue = string.Empty;
                            }
                            if (rowStringInfo.CurrentColumnIndex + 1 < rowStringInfo.ColumnCount)
                            {
                                this.ParsingFunction = WaitForNextColumn;
                                rowStringInfo.CurrentStringIndex++;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        break;
                    }
                default:
                    {
                        rowStringInfo.CurrentColumnValue += rowStringInfo.GetCurrentCharacter();
                        rowStringInfo.CurrentStringIndex++;
                        break;
                    }
            }
            return false;
        }

        private bool WaitForNextColumn(RowStringInfo rowStringInfo)
        {
            switch (rowStringInfo.GetCurrentCharacterAsLower())
            {
                case ',':
                    {
                        if (rowStringInfo.CurrentColumnIndex + 1 < rowStringInfo.ColumnCount)
                        {
                            rowStringInfo.CurrentStringIndex++;
                            rowStringInfo.CurrentColumnIndex++;
                            this.ParsingFunction = this.Start;
                        }
                        else
                        {
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }

        private bool NonQuotedValueCapture(RowStringInfo rowStringInfo)
        {
            switch (rowStringInfo.GetCurrentCharacterAsLower())
            {
                case ',':
                    {
                        if (rowStringInfo.CurrentColumnIndex + 1 < rowStringInfo.ColumnCount)
                        {
                            rowStringInfo.CurrentStringIndex++;
                            rowStringInfo.CurrentColumnIndex++;
                            this.ParsingFunction = this.Start;
                        }
                        else
                        {
                            return true;
                        }
                        break;
                    }
                default:
                    {
                        rowStringInfo.CurrentColumnValue += rowStringInfo.GetCurrentCharacter();
                        if (rowStringInfo.CurrentStringIndex + 1 < rowStringInfo.Length)
                        {
                            rowStringInfo.CurrentStringIndex++;
                        }
                        else
                        {
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }

        #endregion
    }

    internal class RowStringInfo
    {
        private int _currentStringIndex;
        private int _currentColumnIndex;

        public string RowString { get; }
        public int Length { get; }
        public int CurrentStringIndex
        {
            get { return this._currentStringIndex; }
            set
            {
                if (value < Length)
                {
                    this._currentStringIndex = value;
                }
                else
                {
                    throw new Exception($"Failed to index into row definition string. Row string does not contain {value + 1} characters. Current Column Index: {this.CurrentColumnIndex}. Row definition: {this.RowString}");
                }
            }
        }
        public int ColumnCount { get; }
        public int CurrentColumnIndex
        {
            get { return this._currentColumnIndex; }
            set
            {
                if (value < ColumnCount)
                {
                    this._currentColumnIndex = value;
                }
            }
        }
        public string CurrentColumnValue
        {
            get { return this.ColumnValues[this._currentColumnIndex]; }
            set
            {
                this.ColumnValues[this._currentColumnIndex] = value;
            }
        }
        public string[] ColumnValues { get; private set; }

        public RowStringInfo(string rowString, int columnCount)
        {
            this.RowString = rowString;
            this.Length = rowString.Length;
            this.ColumnCount = columnCount;
            this.CurrentStringIndex = 0;
            this.CurrentColumnIndex = 0;
            this.ColumnValues = new string[columnCount];
        }

        public char GetCurrentCharacter() => this.RowString[this.CurrentStringIndex];
        public char GetCurrentCharacterAsLower() => char.ToLower(this.RowString[this.CurrentStringIndex]);
        public bool TryPreviewNextCharacter(out char nextCharacter)
        {
            if (this.CurrentStringIndex + 1 < this.Length)
            {
                nextCharacter = this.RowString[this.CurrentStringIndex + 1];
                return true;
            }
            else
            {
                nextCharacter = '\0';
                return false;
            }
        }
    }
}