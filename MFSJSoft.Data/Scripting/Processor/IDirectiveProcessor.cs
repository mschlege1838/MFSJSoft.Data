﻿

using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting.Processor
{
    public interface IDirectiveProcessor
    {

        DirectiveInitialization InitDirective(CompositeProcessorContext context, ScriptDirective directive);

        DirectiveInitialization SetupDirective(CompositeProcessorContext context, ScriptDirective directive, object initState);

        bool TryExecute(CompositeProcessorContext context, string text, ScriptDirective directive, object initState);

    }

}
