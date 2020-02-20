namespace DataScriptr.Library.Parsing
{
    public interface IRowParserState
    {
        void ParseCharacter(string rowString, int currentCharacterIndex);
    }
}