using System;

using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting
{
    public class InvalidDirectiveException : Exception
    {
        public InvalidDirectiveException(string message, ScriptDirective directive) : base($"{message} ({directive.FileName}: {directive.LineNumber})")
        {
            Error = message;
            Directive = directive;
        }

        public string Error { get; }

        public ScriptDirective Directive { get; }
    }
}
