using System;
using System.Collections.Generic;

using MFSJSoft.Data.Scripting.Model;
using MFSJSoft.Data.Scripting.Parser;

namespace MFSJSoft.Data.Scripting
{

    /// <summary>
    /// Represents the source code for a given SQL script.
    /// </summary>
    /// <remarks>
    /// Stores basic information about the SQL script, including its full <see cref="Source"/> code
    /// as a <see cref="string"/>, optionally the <see cref="Name"/> of the file from which it came
    /// (defaults to <c>&lt;input&gt;</c>), and optionally a <see cref="StatementTerminator"/> used to
    /// split statements within the script. Defaults to <see cref="DefaultStatementTerminator"/> (the
    /// semicolon).
    /// </remarks>
    public class ScriptSource
    {

        /// <summary>
        /// Default character sequence used to separate statements within a script. Its value is the
        /// semicolon (<c>;</c>).
        /// </summary>
        public const string DefaultStatementTerminator = ";";

        /// <summary>
        /// Construct a new <see cref="ScriptSource"/> with the given information.
        /// </summary>
        /// <param name="source">The full script source code as a <see cref="string"/></param>
        /// <param name="name">The name/file name for this script. (Default = <c>&lt;input&gt;</c></param>
        /// <param name="statementTerminator">Custom statement terminator this script uses to separate statements.</param>
        public ScriptSource(string source, string name = "<input>", string statementTerminator = DefaultStatementTerminator)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Name = name;
            StatementTerminator = statementTerminator ?? DefaultStatementTerminator;
            
            foreach (var ch in ScriptLexer.SigChars)
            {
                if (StatementTerminator.IndexOf(ch) != -1)
                {
                    throw new ArgumentException($"Terminator cannot contain the character: {ch}", nameof(statementTerminator));
                }
            }
        }

        /// <summary>
        /// Name of the script represented.
        /// </summary>
        /// <remarks>
        /// When the script is compiled, this is treated as the file name.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        /// Script source code as a <see cref="string"/>.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Character sequence used to separate individual statements within the script.
        /// </summary>
        public string StatementTerminator { get; }

        /// <summary>
        /// Parse this <see cref="ScriptSource"/> into an order <see cref="IList{T}">list</see> of
        /// <see cref="ScriptStatement">ScriptStatements</see>.
        /// </summary>
        /// <returns>A <see cref="IList{T}">list</see> of <see cref="ScriptStatement">statements</see> in the
        /// order they appear in the script.</returns>
        public IList<ScriptStatement> Parse()
        {
            var lexer = new ScriptLexer(Source, Name, StatementTerminator);
            var parser = new ScriptParser(lexer);
            return parser.ScriptFile();
        }

    }
}
