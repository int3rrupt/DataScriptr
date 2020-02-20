namespace DataScriptr.Library.Parsing
{
    public class StringCaptureState : IRowParserState
    {

        public StringCaptureState(RowParserStateContext rowParserStateContext)
        {

        }
        public void ParseCharacter(string rowString, int currentCharacterIndex)
        {
            switch (char.ToLower(rowString[currentCharacterIndex]))
            {
                case '\'':
                    {

                        break;
                    }
            }
        }
    }
}