
namespace MFSJSoft.Data.Scripting.Processor
{
    public class IfProcessor : PropertiesReplaceProcessor
    {

        public const string DirectiveName = "If";
        public const string NegatedDirectiveName = "IfNot";

        public IfProcessor(IProperties properties) : base(DirectiveName, NegatedDirectiveName, properties, true)
        {

        }

    }
}
