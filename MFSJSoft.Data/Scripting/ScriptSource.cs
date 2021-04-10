using System;
using System.Collections.Generic;

using MFSJSoft.Data.Scripting.Model;
using MFSJSoft.Data.Scripting.Parser;

namespace MFSJSoft.Data.Scripting
{
    public class ScriptSource
    {

        public static readonly string DefaultStatementTerminator = ";";

        public ScriptSource(string source, string name = null, string statementTerminator = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Name = name ?? "<input>";
            
            if (string.IsNullOrEmpty(statementTerminator))
            {
                StatementTerminator = DefaultStatementTerminator;
            }
            else
            {
                foreach (var ch in ScriptLexer.SigChars)
                {
                    if (statementTerminator.IndexOf(ch) != -1)
                    {
                        throw new ArgumentException($"Terminator cannot contain the character: {ch}", nameof(statementTerminator));
                    }
                }
            }
        }

        public string Name { get; }
        public string Source { get; }
        public string StatementTerminator { get; }

        public IList<ScriptStatement> Parse()
        {
            var lexer = new ScriptLexer(Source, Name, StatementTerminator);
            var parser = new ScriptParser(lexer);
            return parser.ScriptFile();
        }

    }
}
