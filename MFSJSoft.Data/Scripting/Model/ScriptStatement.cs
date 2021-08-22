using System.Collections.Generic;

namespace MFSJSoft.Data.Scripting.Model
{

    /// <summary>
    /// Represents the full contents of an individual statement within a script, including the statement
    /// <see cref="Text"/> (all whitespace collapsed), all declared <see cref="Directives"/>, along with
    /// the <see cref="FileName"/> and starting <see cref="LineNumber"/> on which the statement is specified.
    /// </summary>
    public class ScriptStatement
    {
        internal ScriptStatement(string text, string fileName, int lineNumber, IDictionary<string, ScriptDirective> directives)
        {
            Text = text;
            FileName = fileName;
            LineNumber = lineNumber;
            Directives = directives;
        }

        /// <summary>
        /// The relevant SQL statement text.
        /// </summary>
        /// <remarks>
        /// All consecutive whitespace characters are collapsed to a single space (<c>' '</c>/<c>0x20</c>), all comments are
        /// stripped, and all directives are either stripped, or replaced with the <see cref="IScriptProcessor">processor</see>-
        /// requested value.
        /// </remarks>
        public string Text { get; }

        /// <summary>
        /// The name of the file in which the statement appears.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The starting line number of the file in which the statement appears.
        /// </summary>
        public int LineNumber { get; }


        /// <summary>
        /// All declared directives within the statement, by generated directive ID.
        /// </summary>
        /// <remarks>
        /// All directives are assigned a generated ID at compiliation time, and are repalced with this within
        /// statement text. By default, this is replaced with the empty string prior to the text being passed
        /// back to application code, however <see cref="IScriptProcessor">Processors</see> can specify a defined
        /// replacement. This can be done "statically," the first time the script is compiled or "dynamically,"
        /// each time the script is executed.
        /// </remarks>
        public IDictionary<string, ScriptDirective> Directives { get; }
    }

}

