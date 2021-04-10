using System;

using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting
{
    public class UnrecognizedDirectiveException : Exception
    {
        
        public UnrecognizedDirectiveException(ScriptDirective directive) : base($"Unrecognized directive: {directive}")
        {
            Directive = directive;
        }

        public ScriptDirective Directive { get; }
    }
}
