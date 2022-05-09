
namespace MFSJSoft.Data.Scripting.Processor
{

    /// <summary>
    /// Dynamic properties replace directive. Properties are evaluated every time a statement is ran.
    /// </summary>
    public class IfProcessor : PropertiesReplaceProcessor
    {

        /// <summary>
        /// Conditional dynamic replace directive name.
        /// </summary>
        public const string DirectiveName = "If";

        /// <summary>
        /// Negated directive name.
        /// </summary>
        public const string NegatedDirectiveName = "IfNot";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="properties">Dynamic properties.</param>
        public IfProcessor(IProperties properties) : base(DirectiveName, NegatedDirectiveName, properties, true)
        {

        }

    }
}
