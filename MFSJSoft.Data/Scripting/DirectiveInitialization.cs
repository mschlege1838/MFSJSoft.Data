
using System;

namespace MFSJSoft.Data.Scripting
{

    /// <summary>
    /// Enumeration of actions to be taken by <see cref="ScriptExecutor"/> for returned <see cref="DirectiveInitialization"/> instances
    /// from <see cref="IScriptProcessor.InitDirective"/> and <see cref="IScriptProcessor.SetupDirective"/>.
    /// </summary>
    [Flags]
    public enum DirectiveInitializationAction
    {
        /// <summary>
        /// Default action. Assumes all initialization is performed in <see cref="IScriptProcessor.InitDirective"/>, and
        /// <see cref="DirectiveInitialization.InitializedState"/> is stored for use in <see cref="IScriptProcessor.ExecuteStatement"/>.
        /// </summary>
        /// <remarks>
        /// The underlying value of this is <c>0</c>, so if only the default action is desired, it need not be specified.
        /// </remarks>
        Default = 0,

        /// <summary>
        /// Indicates the directive should not be stored for use in <see cref="IScriptProcessor.ExecuteStatement"/>
        /// </summary>
        NoStore = 1,

        /// <summary>
        /// Replace the directive with <see cref="DirectiveInitialization.ReplacementText"/> within the statement text where it
        /// was declared.
        /// </summary>
        ReplaceText = 2,

        /// <summary>
        /// Indicates the directive should be passed to <see cref="IScriptProcessor.SetupDirective"/> prior to <see cref="IScriptProcessor.ExecuteStatement"/>
        /// at execution time.
        /// </summary>
        DeferSetup = 4
    }

    /// <summary>
    /// Directive initialization information to be returned from <see cref="IScriptProcessor.InitDirective"/> and <see cref="IScriptProcessor.SetupDirective"/>
    /// </summary>
    /// <seealso cref="IScriptProcessor"/>
    public class DirectiveInitialization
    {

        /// <summary>
        /// Solitary constructor; all arguments ar optional, and default to the standard <see langword="default" />.
        /// </summary>
        /// <param name="initializedState">State object to be passed to <see cref="IScriptProcessor.ExecuteStatement"/> when the script is
        /// being executed. Also passed to <see cref="IScriptProcessor.SetupDirective"/> if <see cref="DirectiveInitializationAction.DeferSetup"/>
        /// is specified.</param>
        /// <param name="action">Combination of zero or more <see cref="DirectiveInitializationAction"/> flags.</param>
        /// <param name="replacementText">Text with whicht to replace the directive within statement text if <see cref="DirectiveInitializationAction.ReplaceText"/>
        /// is specified.</param>
        public DirectiveInitialization(object initializedState = default, DirectiveInitializationAction action = default, string replacementText = default)
        {
            Action = action;
            ReplacementText = replacementText;
            InitializedState = initializedState;
        }


        /// <summary>
        /// Combination of zero or more <see cref="DirectiveInitializationAction"/> flags.
        /// </summary>
        public DirectiveInitializationAction Action { get; set; }

        /// <summary>
        /// Text with whicht to replace the directive within statement text if <see cref="DirectiveInitializationAction.ReplaceText"/>
        /// is specified.
        /// </summary>
        public string ReplacementText { get; set; }

        /// <summary>
        /// State object to be passed to <see cref="IScriptProcessor.ExecuteStatement"/> when the script is
        /// being executed. Also passed to <see cref="IScriptProcessor.SetupDirective"/> if <see cref="DirectiveInitializationAction.DeferSetup"/>
        /// is specified.
        /// </summary>
        public object InitializedState { get; set; }

    }
}
