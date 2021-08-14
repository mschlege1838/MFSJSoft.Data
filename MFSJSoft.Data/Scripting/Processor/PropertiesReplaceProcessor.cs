using MFSJSoft.Data.Scripting.Model;
using System;

namespace MFSJSoft.Data.Scripting.Processor
{
    public class PropertiesReplaceProcessor : PropertiesEvaluator, IDirectiveProcessor
    {

        readonly string directiveName;
        readonly string negatedDirectiveName;
        readonly bool deferRuntime;

        public PropertiesReplaceProcessor(string directiveName, string negatedDirectiveName, IProperties properties, bool deferRuntime) : base(properties)
        {
            this.directiveName = string.IsNullOrEmpty(directiveName) ? throw new ArgumentNullException(nameof(directiveName)) : directiveName;
            this.negatedDirectiveName = string.IsNullOrEmpty(negatedDirectiveName) ? throw new ArgumentNullException(nameof(negatedDirectiveName)) : negatedDirectiveName;
            this.deferRuntime = deferRuntime;
        }

        public void InitProcessor(object configuration)
        {
            Init(configuration);
        }

        public DirectiveInitialization InitDirective(CompositeProcessorContext context, ScriptDirective directive)
        {
            bool negated;
            if (directive.Name == directiveName)
            {
                negated = false;
            }
            else if (directive.Name == negatedDirectiveName)
            {
                negated = true;
            }
            else
            {
                return null;
            }

            if (directive.Arguments.Count < 2)
            {
                throw new InvalidDirectiveException($"{directiveName} must have at least 2 arguments (property name, replace value).", directive);
            }
            if (directive.Arguments.Count > 4)
            {
                throw new InvalidDirectiveException($"{directiveName} can have at most 4 arguments.", directive);
            }

            var propertyName = directive.Arguments[0];
            var propertyValue = directive.Arguments.Count > 2 ? directive.Arguments[1] : null;
            var replaceValue = directive.Arguments[directive.Arguments.Count > 2 ? 2 : 1];
            var elseValue = directive.Arguments.Count > 3 ? directive.Arguments[3] : null;

            if (deferRuntime)
            {
                return new DirectiveInitialization(new InitializedState(propertyName, replaceValue, propertyValue, elseValue, negated), DirectiveInitializationAction.DEFER_SETUP);
            }

            if (Eval(propertyName, propertyValue, negated))
            {
                return new DirectiveInitialization(action: DirectiveInitializationAction.REPLACE_TEXT | DirectiveInitializationAction.NO_STORE, replacementText: replaceValue);
            }
            else
            {
                if (elseValue is not null)
                {
                    return new DirectiveInitialization(action: DirectiveInitializationAction.REPLACE_TEXT | DirectiveInitializationAction.NO_STORE, replacementText: elseValue);
                }
                else
                {
                    return new DirectiveInitialization(action: DirectiveInitializationAction.NO_STORE);
                }
            }
        }

        public DirectiveInitialization SetupDirective(CompositeProcessorContext context, ScriptDirective directive, object initState)
        {
            if (!deferRuntime)
            {
                throw new NotImplementedException();
            }

            var initData = (InitializedState) initState;
            if (Eval(initData.PropertyName, initData.PropertyValue, initData.Negated))
            {
                return new DirectiveInitialization(action: DirectiveInitializationAction.REPLACE_TEXT, replacementText: initData.ReplaceValue);
            }
            else
            {
                if (initData.ElseValue is not null)
                {
                    return new DirectiveInitialization(action: DirectiveInitializationAction.REPLACE_TEXT, replacementText: initData.ElseValue);
                }
                else
                {
                    return new DirectiveInitialization();
                }
            }
        }

        public bool TryExecute(CompositeProcessorContext context, string text, ScriptDirective directive, object initState)
        {
            return false;
        }

        class InitializedState
        {
            internal InitializedState(string propertyName, string replaceValue, string propertyValue, string elseValue, bool negated)
            {
                PropertyName = propertyName;
                ReplaceValue = replaceValue;
                PropertyValue = propertyValue;
                ElseValue = elseValue;
                Negated = negated;
            }

            internal string PropertyName { get; }
            internal string ReplaceValue { get; }
            internal string PropertyValue { get; }
            internal string ElseValue { get; }
            internal bool Negated { get; }
        }
    }
}
