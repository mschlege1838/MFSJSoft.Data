using MFSJSoft.Data.Scripting.Model;
using System;


namespace MFSJSoft.Data.Scripting.Processor
{
    public class CallbackProcessor : IDirectiveProcessor
    {

        public const string DirectiveName = "Callback";

        public delegate bool ExecuteStatement(string statementName, string text);


        readonly ExecuteStatement callback;

        public CallbackProcessor(ExecuteStatement callback)
        {
            this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public void InitProcessor(CompositeProcessorContext context, object config)
        {

        }

        public DirectiveInitialization InitDirective(CompositeProcessorContext context, ScriptDirective directive)
        {
            if (directive.Name != DirectiveName)
            {
                return null;
            }
            if (directive.Arguments.Count != 1)
            {
                throw new InvalidDirectiveException("Callback directive must have exactly 1 argument.", directive);
            }

            return new DirectiveInitialization(new InitializedState(directive.Arguments[0]));
        }

        public DirectiveInitialization SetupDirective(CompositeProcessorContext context, ScriptDirective directive, object initState)
        {
            throw new NotImplementedException();
        }

        public bool TryExecute(CompositeProcessorContext context, string text, ScriptDirective directive, object initState)
        {
            if (directive.Name != DirectiveName)
            {
                return false;
            }

            var statementName = ((InitializedState) initState).StatementName;
            if (!callback(statementName, text))
            {
                throw new UnrecognizedStatementException(statementName, directive);
            }

            return true;
        }
        
        class InitializedState
        {
            internal InitializedState(string statementName)
            {
                StatementName = statementName;
            }

            internal string StatementName { get; }
        }
    }

    public class UnrecognizedStatementException : Exception
    {

        internal UnrecognizedStatementException(string statementName, ScriptDirective directive) : base($"Unrecognized statement name: {statementName}; Directive: {directive}")
        {
            StatementName = statementName;
            Directive = directive;
        }

        public string StatementName { get; }

        public ScriptDirective Directive { get; }
    }
}
