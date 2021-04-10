
using System;

namespace MFSJSoft.Data.Scripting.Parser
{
    public class ScriptSyntaxError : Exception
    {
        
        internal ScriptSyntaxError(string message, string fileName, int lineNumber, int columnNumber) : base($"{message} ({fileName}: {lineNumber},{columnNumber})")
        {
            FileName = fileName;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public string FileName { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }
    }

}

