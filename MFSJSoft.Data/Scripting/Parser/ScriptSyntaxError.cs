
using System;

namespace MFSJSoft.Data.Scripting.Parser
{

    /// <summary>
    /// Thrown by the <see cref="ScriptSource.Parse" /> when there is a syntax error within a directive.
    /// </summary>
    public class ScriptSyntaxError : Exception
    {
        
        internal ScriptSyntaxError(string message, string fileName, int lineNumber, int columnNumber) : base($"{message} ({fileName}: {lineNumber},{columnNumber})")
        {
            FileName = fileName;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        /// <summary>
        /// Name of the source file in which the syntax error occurred.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Line number of the statement on which the error occurred.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// First column number of the line in which the error occurred.
        /// </summary>
        public int ColumnNumber { get; }
    }

}

