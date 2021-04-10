
using System.Collections.Generic;

using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting
{
    public interface IScriptProcessor
    {

        DirectiveInitialization InitDirective(ScriptDirective directive);

        DirectiveInitialization SetupDirective(ScriptDirective directive, object initState);

        void ExecuteStatement(string text, IList<(ScriptDirective, object)> directives);

    }

    
}
