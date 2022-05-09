using MFSJSoft.Data.Scripting.Model;
using System;


namespace MFSJSoft.Data.Scripting.Processor
{

    /// <summary>
    /// Directive processor for the <c>Callback</c> directive.
    /// </summary>
    public class CallbackProcessor : IDirectiveProcessor
    {

        /// <summary>
        /// This directive's name
        /// </summary>
        public const string DirectiveName = "Callback";

        /// <summary>
        /// Callback this processor executes when relevent statements are being processed.
        /// </summary>
        /// <param name="statementName">Name of statement givne in directive</param>
        /// <param name="text">SQL statement text.</param>
        /// <returns></returns>
        public delegate bool ExecuteStatement(string statementName, string text);


        readonly ExecuteStatement callback;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="callback">Callback to execute statements annotated with this directive.</param>
        /// <exception cref="ArgumentNullException">If <c>callback</c> is <c>null</c></exception>
        public CallbackProcessor(ExecuteStatement callback)
        {
            this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <summary>
        /// No-op.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="config"></param>
        public void InitProcessor(CompositeProcessorContext context, object config)
        {

        }

        /// <summary>
        /// Initializes this directive.
        /// </summary>
        /// <param name="context">DB context.</param>
        /// <param name="directive">Directive to test.</param>
        /// <returns><c>null</c> if not applicable, otherwise this directive's initialization state</returns>
        /// <exception cref="InvalidDirectiveException">If the number of argments provided to this directive is not exactly <c>1</c></exception>
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

        /// <summary>
        /// N/A
        /// </summary>
        /// <param name="context"></param>
        /// <param name="directive"></param>
        /// <param name="initState"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public DirectiveInitialization SetupDirective(CompositeProcessorContext context, ScriptDirective directive, object initState)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes this directive, if applicable.
        /// </summary>
        /// <param name="context">DB Context.</param>
        /// <param name="text">SQL statement text</param>
        /// <param name="directive">Source information.</param>
        /// <param name="initState">Initialization state.</param>
        /// <returns><c>true</c> if this processor can execute the current statement, otherwise <c>false</c></returns>
        /// <exception cref="UnrecognizedStatementException">If this directive's <c>callback</c> returns <c>false</c></exception>
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

    /// <summary>
    /// Thrown when a <see cref="CallbackProcessor"/>'s callback encounters a statement name that is not
    /// recognized. (I.e. returns <c>false</c>)
    /// </summary>
    public class UnrecognizedStatementException : Exception
    {

        internal UnrecognizedStatementException(string statementName, ScriptDirective directive) : base($"Unrecognized statement name: {statementName}; Directive: {directive}")
        {
            StatementName = statementName;
            Directive = directive;
        }

        /// <summary>
        /// Name of the statement as provided in the SQL script.
        /// </summary>
        public string StatementName { get; }

        /// <summary>
        /// Source information.
        /// </summary>
        public ScriptDirective Directive { get; }
    }
}
