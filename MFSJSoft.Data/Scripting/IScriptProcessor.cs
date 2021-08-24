
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting
{
    public interface IScriptProcessor
    {

        void InitProcessor(object configuration, ILogger logger);

        DirectiveInitialization InitDirective(ScriptDirective directive);

        DirectiveInitialization SetupDirective(ScriptDirective directive, object initState);

        void ExecuteStatement(string text, IList<(ScriptDirective, object)> directives);

    }

    
}
