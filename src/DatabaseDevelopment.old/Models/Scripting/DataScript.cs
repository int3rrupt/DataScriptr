using DatabaseDevelopment.Enums;

namespace DatabaseDevelopment.Models.Scripting
{
    public struct DataScript
    {
        public string Name { get; set; }
        public DataScriptType Type { get; set; }
        public DatabaseEnvironment Environment { get; set; }
        public string Path { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}