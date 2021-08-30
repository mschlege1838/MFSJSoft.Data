
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using MFSJSoft.Data.Scripting.Model;
using MFSJSoft.Data.Scripting.Processor;

namespace MFSJSoft.Data.Scripting
{
    /// <summary>
    /// Low-level processor <see cref="ScriptExecutor"/> uses to <see cref="ScriptExecutor.ExecuteScript">execute</see>
    /// scripts. Generally the only implementation application code will use is the <see cref="CompositeProcessor"/>.
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
    ///         <see cref="IIdentifiable"/>, and the global <see cref="ILogger"/> associated with this context. Both the global
    ///         config and <see cref="ILogger"/> are constructor arguments to 
    ///         <see cref="ScriptExecutor(IScriptResolver, IDictionary{object, object}, ILogger)">ScriptExecutor</see>, and both can
    ///         be <see langword="null"/> for a given <see cref="IScriptProcessor"/>.
    ///     </description>
    ///     </item>
    ///     
    ///     <item>
    ///     <description>
    ///         <see cref="SetupDirective"/> is called once for each directive in declaration order for each script(name) passed to
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
    ///         <see cref="SetupDirective"/> is called for each directive in declaration order every time a script is executed, unless
    ///         the <see cref="DirectiveInitializationAction.NO_STORE"/> action was specified on the return value of <see cref="InitDirective"/>.
    ///         Subsequent behavior based on the returned <see cref="DirectiveInitialization"/> is defined in detail below.
    ///     </description>
    ///     </item>
    ///     
    ///     <item>
    ///     <description>
    ///         <see cref="ExecuteStatement"/> is called for each statement in the script with the resolved statement text and an ordered sequence
    ///         of tuples. The tuples follow directive declaration order. The first value of each tuple is the <see cref="ScriptDirective"/> 
    ///         in question, coupled with its final initialization state as determined by <see cref="InitDirective"/> and
    ///         <see cref="SetupDirective"/> (the second value). If <see cref="DirectiveInitializationAction.NO_STORE"/> was specified for a directive
    ///         in either <see cref="InitDirective"/> or <see cref="SetupDirective"/>, it will not be present in the list.
    ///     </description>
    ///     </item>
    /// </list>
    /// 
    /// 
    /// </remarks>
    public interface IScriptProcessor
    {

        void InitProcessor(object configuration, ILogger logger);

        DirectiveInitialization InitDirective(ScriptDirective directive);

        DirectiveInitialization SetupDirective(ScriptDirective directive, object initState);

        void ExecuteStatement(string text, IList<(ScriptDirective, object)> directives);

    }

    
}
