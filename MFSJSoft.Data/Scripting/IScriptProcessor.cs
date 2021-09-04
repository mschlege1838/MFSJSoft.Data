
using System;
using System.Data.Common;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using MFSJSoft.Data.Scripting.Model;
using MFSJSoft.Data.Scripting.Processor;

namespace MFSJSoft.Data.Scripting
{
    /// <summary>
    /// Low-level processor <see cref="ScriptExecutor"/> uses to <see cref="ScriptExecutor.ExecuteScript">execute</see>
    /// scripts. Generally the only implementation applications will use is <see cref="CompositeProcessor"/>.
    /// </summary>
    /// <remarks>
    /// <para>Implement <see cref="IScriptProcessor"/> to customize how scripts are <see cref="ScriptExecutor.ExecuteScript">executed</see>
    /// by <see cref="ScriptExecutor"/>.</para>
    /// 
    /// <para>Static script initialization methods are called as follows. This call sequence occurs once per unique script name.</para>
    /// <list type="number">
    ///     <item>
    ///     <description>
    ///         <see cref="InitProcessor"/> is called with the global config value having a key that matches this processor
    ///         by its runtime <see cref="object.GetType">type</see> or <see cref="IIdentifiable.Id">id</see>, if it also impelments
    ///         <see cref="IIdentifiable"/>, and the global <see cref="ILogger"/> associated with the parent <see cref="ScriptExecutor"/>.
    ///         Both the global config and <see cref="ILogger"/> are constructor arguments to 
    ///         <see cref="ScriptExecutor(IScriptResolver, IDictionary{object, object}, ILogger)">ScriptExecutor</see>, and both can
    ///         be <see langword="null"/> for a given <see cref="IScriptProcessor"/>.
    ///     </description>
    ///     </item>
    ///     
    ///     <item>
    ///     <description>
    ///         <see cref="InitDirective"/> is called once for each directive in declaration order for each unique script passed to
    ///         <see cref="ScriptExecutor.ExecuteScript"/>. Static initialization should be performed here, as this will only be
    ///         called the first time a new script name is encountered. Subsequent behavior based on the returned <see cref="DirectiveInitialization"/>
    ///         is defined in detail below.
    ///     </description>
    ///     </item>
    /// </list>
    /// 
    /// <para>Dynamic script execution methods are called as follows. This call sequence occurs every time a script is executed.</para>
    /// <list type="number">
    ///     <item>
    ///     <description>
    ///         <see cref="SetupDirective"/> is called for each directive in declaration order every time a script is executed, for each
    ///         directive that specified the <see cref="DirectiveInitializationAction.DEFER_SETUP"/> action in <see cref="InitDirective"/>.
    ///         The initialized state from <see cref="InitDirective"/> is also passed. Subsequent behavior based on the returned 
    ///         <see cref="DirectiveInitialization"/> is defined in detail below.
    ///     </description>
    ///     </item>
    ///     
    ///     <item>
    ///     <description>
    ///         <see cref="ExecuteStatement"/> is called for each statement in the script with the resolved statement text and an ordered sequence
    ///         of tuples containing directive information. These follow directive declaration order. Each contains the <see cref="ScriptDirective"/> 
    ///         in question, coupled with its final initialization <see cref="DirectiveInitialization.InitializedState">state</see> as determined by 
    ///         <see cref="InitDirective"/> and (if applicable) <see cref="SetupDirective"/>. If <see cref="DirectiveInitializationAction.NO_STORE"/> 
    ///         was specified for a directive in either <see cref="InitDirective"/> or <see cref="SetupDirective"/>, it will not be present in the list.
    ///     </description>
    ///     </item>
    /// </list>
    /// 
    /// <para><see cref="DirectiveInitialization"/> values returned from <see cref="InitDirective"/> and <see cref="SetupDirective"/> determine how
    /// the directive is handled by <see cref="ScriptExecutor"/>. This is largely dictated by the <see cref="DirectiveInitialization.Action"/>
    /// property:</para>
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="DirectiveInitializationAction.DEFAULT"/></term>
    ///         <description>The default action is to assume all setup is performed in <see cref="InitDirective"/>, and there is no need to
    ///         call <see cref="SetupDirective"/> at execution time. The directive and initialization <see cref="DirectiveInitialization.InitializedState">state</see>
    ///         will be stored, and present in its statement's directive list when <see cref="ExecuteStatement"/> is called.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="DirectiveInitializationAction.NO_STORE"/></term>
    ///         <description>When specified on the return value from <see cref="InitDirective"/> or <see cref="SetupDirective"/>, the directive 
    ///         and its state will not be saved. It will not be present when <see cref="ExecuteStatement"/> is called.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="DirectiveInitializationAction.REPLACE_TEXT"/></term>
    ///         <description>When specified on the return value from <see cref="InitDirective"/> or <see cref="SetupDirective"/>, the directive
    ///         will be replaced with the <see cref="DirectiveInitialization.ReplacementText"/> property within the statement text.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="DirectiveInitializationAction.DEFER_SETUP"/></term>
    ///         <description>When specified on the return value from <see cref="InitDirective"/>, the directive will stored, and passed to
    ///         <see cref="SetupDirective"/> at execution time, subsequent to the script being executed. Specifing this on the return value from 
    ///         <see cref="SetupDirective"/> has no effect. It is an error to specify this action with
    ///         <see cref="DirectiveInitializationAction.NO_STORE"/>.</description>
    ///     </item>
    /// </list>
    /// 
    /// </remarks>
    public interface IScriptProcessor
    {

        /// <summary>
        /// Initalize this processor with its global configuration and the global <see cref="ILogger"/>. Both can be
        /// <see langword="null" />.
        /// </summary>
        /// <remarks>
        /// When the global <see cref="ILogger"/> is <see langword="null"/>, logging should generally be written to the <see cref="Console"/>,
        /// typically filtered down to only warnings and errors.
        /// </remarks>
        /// <param name="configuration">Global configuration value matching this processor. Can be <see langword="null" /></param>
        /// <param name="logger">Global <see cref="ILogger"/> associated with the parent <see cref="ScriptExecutor"/>.
        /// Can be <see langword="null" />.</param>
        void InitProcessor(object configuration, ILogger logger);


        /// <summary>
        /// Perform initialization for the given <see cref="ScriptDirective"/>, and return any state to be stored for subsequent 
        /// processing (if any). Only called once per statement, per directive.
        /// </summary>
        /// <param name="directive"><see cref="ScriptDirective"/> to be initialized.</param>
        /// <returns><see cref="DirectiveInitialization"/> instructions on how to handle the directive, and state to be
        /// stored for subsequent execution (if any).</returns>
        DirectiveInitialization InitDirective(ScriptDirective directive);

        /// <summary>
        /// Perform any setup needed by the <see cref="ScriptDirective"/>, prior to script execution. Called for each directive
        /// every time a script is executed. Only called for directives that specified the <see cref="DirectiveInitializationAction.DEFER_SETUP"/>
        /// action in <see cref="InitDirective"/>.
        /// </summary>
        /// <param name="directive"><see cref="ScriptDirective"/> requiring setup.</param>
        /// <param name="initState"><see cref="DirectiveInitialization.InitializedState"/> from <see cref="InitDirective"/></param>
        /// <returns></returns>
        DirectiveInitialization SetupDirective(ScriptDirective directive, object initState);

        /// <summary>
        /// Execute the given statement based on its text and list of initialized directives.
        /// </summary>
        /// <remarks>
        /// Only the final, resolved statement text and its final directive list are passed to this method. It is up to implementors
        /// to create and execute the necessary <see cref="DbCommand"/>, etc. No checks are performed to see whether the statement
        /// is actually executed against the database.
        /// </remarks>
        /// <param name="text">Final, resolved statement text, after all <see cref="DirectiveInitializationAction.REPLACE_TEXT"/>
        /// actions are processed.</param>
        /// <param name="directives">List of directives associated with the given statement with their final initialization 
        /// <see cref="DirectiveInitialization.InitializedState">state</see>.</param>
        void ExecuteStatement(string text, IList<(ScriptDirective, object)> directives);

    }

    
}
