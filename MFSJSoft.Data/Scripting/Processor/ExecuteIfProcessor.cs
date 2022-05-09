
using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting.Processor
{

    /// <summary>
    /// Processor for conditional execution of statements. Will cause SQL statements on which it is declared
    /// to only be executed if the configured property name evaluates to <see langword="true"/>, or (optionally)
    /// is equal to a provided value. Opposite behavior if negated.
    /// </summary>
    public class ExecuteIfProcessor : PropertiesEvaluator, IDirectiveProcessor
    {

        /// <summary>
        /// Conditional execution directive name.
        /// </summary>
        public const string DirectiveName = "ExecuteIf";

        /// <summary>
        /// Negated directive name.
        /// </summary>
        public const string NegatedDirectiveName = "SkipIf";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="properties">Properties to evaluate against configured conditions.</param>
        public ExecuteIfProcessor(IProperties properties) : base(properties)
        {

        }

        /// <summary>
        /// Initializes this processor.
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="configuration">Global config</param>
        public void InitProcessor(CompositeProcessorContext context, object configuration)
        {
            Init(configuration);
        }

        /// <summary>
        /// Initializes this directive as configured in the source script.
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="directive">Source information</param>
        /// <returns>Initialization state if this processor can handle the given directive, otherwise <see langword="null" /></returns>
        /// <exception cref="InvalidDirectiveException">If the provided argument count is less than 1 or greater than 2</exception>
        public DirectiveInitialization InitDirective(CompositeProcessorContext context, ScriptDirective directive)
        {
            bool negated;
            switch (directive.Name)
            {
                case DirectiveName:
                    negated = false;
                    break;
                case NegatedDirectiveName:
                    negated = true;
                    break;
                default:
                    return null;

            }

            if (directive.Name != directive.Name)
            {
                return null;
            }

            if (directive.Arguments.Count < 1)
            {
                throw new InvalidDirectiveException($"{directive.Name} must have at least 1 argument (property name).", directive);
            }
            if (directive.Arguments.Count > 2)
            {
                throw new InvalidDirectiveException($"{directive.Name} can have at most 2 arguments.", directive);
            }

            var propertyName = directive.Arguments[0];
            var value = directive.Arguments.Count > 1 ? directive.Arguments[1] : null;

            return new DirectiveInitialization(new InitializedState(propertyName, value, negated));
        }

        /// <summary>
        /// N/A
        /// </summary>
        /// <param name="context"></param>
        /// <param name="directive"></param>
        /// <param name="initState"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public DirectiveInitialization SetupDirective(CompositeProcessorContext context, ScriptDirective directive, object initState)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Executes this directive against the given context, if applicable.
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="text">SQL statement text</param>
        /// <param name="directive">Source information</param>
        /// <param name="initState">Initialization state</param>
        /// <returns><see langword="true"/> if this processor is able to execute the given directive, otherwise <see langword="false" /></returns>
        public bool TryExecute(CompositeProcessorContext context, string text, ScriptDirective directive, object initState)
        {
            if (directive.Name != DirectiveName && directive.Name != NegatedDirectiveName)
            {
                return false;
            }

            var initData = (InitializedState) initState;

            if (Eval(initData.PropertyName, initData.PropertyValue, initData.Negated))
            {
                var command = context.NewCommand();
                command.CommandText = text;
                command.ExecuteNonQuery();
            }

            return true;
        }


        class InitializedState
        {
            internal InitializedState(string propertyName, string propertyValue, bool negated)
            {
                PropertyName = propertyName;
                PropertyValue = propertyValue;
                Negated = negated;
            }

            internal string PropertyName { get; }
            internal string PropertyValue { get; }
            internal bool Negated { get; }
        }
    }
}
