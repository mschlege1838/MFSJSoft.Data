
namespace MFSJSoft.Data.Scripting.Processor
{
    public class IfDefProcessor : PropertiesReplaceProcessor
    {
        public const string DirectiveName = "IfDef";
        public const string NegatedDirectiveName = "IfNotDef";

        public IfDefProcessor(IProperties properties) : base(DirectiveName, NegatedDirectiveName, properties, false)
        {

        }
    }
}
