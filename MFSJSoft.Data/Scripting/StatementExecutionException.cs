using System;

namespace MFSJSoft.Data.Scripting
{
    /// <summary>
    /// Wrapper exception thrown when the execution of a statement within a script fails.
    /// </summary>
    public class StatementExecutionException : Exception
    {

        internal StatementExecutionException(string text, string fileName, int lineNumber, Exception rootCause) : base($"Error executing statement: {text} ({fileName}:{lineNumber})", rootCause)
        {
            Text = text;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// The statement text, as submitted to the database server.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Script file name in which the error occurred.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// First line number of the statement in which the error occurred.
        /// </summary>
        public int LineNumber { get; }
    }
}
