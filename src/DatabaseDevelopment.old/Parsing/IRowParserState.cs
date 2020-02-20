namespace DatabaseDevelopment.Parsing
{
    public interface IRowParserState
    {
        void ParseCharacter(string rowString, int currentCharacterIndex);
    }
}