
using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting.Processor
{

    public class ExecuteIfProcessor : PropertiesEvaluator, IDirectiveProcessor
    {

        public const string DirectiveName = "ExecuteIf";
        public const string NegatedDirectiveName = "SkipIf";

        public ExecuteIfProcessor(IProperties properties) : base(properties)
        {

        }

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

        public DirectiveInitialization SetupDirective(CompositeProcessorContext context, ScriptDirective directive, object initState)
        {
            throw new System.NotImplementedException();
        }

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
