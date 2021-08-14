
using System.Collections.Generic;

using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting
{
    public interface IScriptProcessor
    {

        void InitProcessor(object configuration);

        DirectiveInitialization InitDirective(ScriptDirective directive);

        DirectiveInitialization SetupDirective(ScriptDirective directive, object initState);

        void ExecuteStatement(string text, IList<(ScriptDirective, object)> directives);

    }

    
}
