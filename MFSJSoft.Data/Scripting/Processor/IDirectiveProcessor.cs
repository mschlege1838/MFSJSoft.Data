

using System.Data.Common;
using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting.Processor
{

    /// <summary>
    /// Analog for <see cref="IScriptProcessor"/> to be used with <see cref="CompositeProcessor"/>. Allows compositing
    /// of individual directive processors.
    /// </summary>
    public interface IDirectiveProcessor
    {

        /// <summary>
        /// Analog for <see cref="IScriptProcessor.InitProcessor"/>.
        /// </summary>
        /// <param name="context"><see cref="CompositeProcessorContext"/> from parent <see cref="CompositeProcessor"/>.</param>
        /// <param name="configuration">Global configuration matching this <see cref="IDirectiveProcessor"/> from
        /// <see cref="CompositeProcessorConfiguration.DirectiveConfiguration"/>, if any.</param>
        void InitProcessor(CompositeProcessorContext context, object configuration);

        /// <summary>
        /// Analog for <see cref="IScriptProcessor.InitDirective"/>.
        /// </summary>
        /// <param name="context"><see cref="CompositeProcessorContext"/> from parent <see cref="CompositeProcessor"/>.</param>
        /// <param name="directive"><see cref="ScriptDirective"/> to be initialized.</param>
        /// <returns><see cref="DirectiveInitialization"/> if this processor supports the given <see cref="ScriptDirective"/>,
        /// otherwise <see langword="null" /></returns>
        /// <exception cref="UnrecognizedDirectiveException">If the given <see cref="ScriptDirective"/> is not supported by this
        /// processor, it is also acceptable to throw this exception.</exception>
        DirectiveInitialization InitDirective(CompositeProcessorContext context, ScriptDirective directive);

        /// <summary>
        /// Analog for <see cref="IScriptProcessor.SetupDirective"/>.
        /// </summary>
        /// <param name="context"><see cref="CompositeProcessorContext"/> from parent <see cref="CompositeProcessor"/>.</param>
        /// <param name="directive"><see cref="ScriptDirective"/> to be set up.</param>
        /// <param name="initState"><see cref="DirectiveInitialization.InitializedState"/> from <see cref="IDirectiveProcessor.InitDirective"/>.</param>
        /// <returns><see cref="DirectiveInitialization"/> if this processor supports the given <see cref="ScriptDirective"/>,
        /// otherwise <see langword="null" /></returns>
        /// <exception cref="UnrecognizedDirectiveException">If the given <see cref="ScriptDirective"/> is not supported by this
        /// processor, it is also acceptable to throw this exception.</exception>
        DirectiveInitialization SetupDirective(CompositeProcessorContext context, ScriptDirective directive, object initState);


        /// <summary>
        /// Analog for <see cref="IScriptProcessor.ExecuteStatement"/>, execpt is is called for each <see cref="ScriptDirective"/> passed
        /// to <see cref="IScriptProcessor.ExecuteStatement"/>. Return <see langword="true"/> if the statement can be/ considered executed,
        /// otherwise <see langword="false" />.
        /// </summary>
        /// <remarks>
        /// This method is called for each <see cref="IDirectiveProcessor"/> associated with the parent <see cref="CompositeProcessor"/>
        /// regardless of individual return values. A generic <see cref="DbCommand"/> is created and
        /// <see cref="DbCommand.ExecuteNonQuery">executed</see> as a non-query if none return <see langword="true" />.
        /// </remarks>
        /// <param name="context"><see cref="CompositeProcessorContext"/> from parent <see cref="CompositeProcessor"/>.</param>
        /// <param name="text">Resolved statement text.</param>
        /// <param name="directive">Current <see cref="ScriptDirective"/>.</param>
        /// <param name="initState">Final <see cref="DirectiveInitialization.InitializedState"/> from <see cref="IDirectiveProcessor.InitDirective"/>
        /// or <see cref="IDirectiveProcessor.SetupDirective"/>, if applicable.</param>
        /// <returns><see langword="true"/> it the statement can be considered executed, otherwise <see langword="false"/></returns>
        bool TryExecute(CompositeProcessorContext context, string text, ScriptDirective directive, object initState);

    }

}
