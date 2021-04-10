using System.Collections.Generic;

namespace MFSJSoft.Data.Scripting.Model
{
    public class ScriptStatement
    {
        internal ScriptStatement(string text, string fileName, int lineNumber, IDictionary<string, ScriptDirective> directives)
        {
            Text = text;
            FileName = fileName;
            LineNumber = lineNumber;
            Directives = directives;
        }

        public string Text { get; }
        public string FileName { get; }
        public int LineNumber { get; }
        public IDictionary<string, ScriptDirective> Directives { get; }
    }

}

