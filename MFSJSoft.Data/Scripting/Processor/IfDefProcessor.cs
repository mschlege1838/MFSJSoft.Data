
namespace MFSJSoft.Data.Scripting.Processor
{

    /// <summary>
    /// Static properties replace directive. Properties are only evaluated during the directive initialization
    /// phase, and retain their value for subsequent execution.
    /// </summary>
    public class IfDefProcessor : PropertiesReplaceProcessor
    {
        /// <summary>
        /// Conditional static replace directive name.
        /// </summary>
        public const string DirectiveName = "IfDef";

        /// <summary>
        /// Negated directive name.
        /// </summary>
        public const string NegatedDirectiveName = "IfNotDef";


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="properties">Optional properties.</param>
        public IfDefProcessor(IProperties properties = null) : base(DirectiveName, NegatedDirectiveName, properties, false)
        {

        }
    }
}
