using System;

using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting
{

    /// <summary>
    /// Thrown by <see cref="IScriptProcessor"/> implementations when invalid directives and/or directive configurations
    /// are found.
    /// </summary>
    public class InvalidDirectiveException : Exception
    {

        /// <summary>
        /// Construct a new <see cref="InvalidDirectiveException"/> with the given error <c>message</c>, and the offending
        /// <see cref="ScriptDirective"/>.
        /// </summary>
        /// <param name="message">Message explaining why the direcitve or its configuration is invalid.</param>
        /// <param name="directive">The invalid directive.</param>
        public InvalidDirectiveException(string message, ScriptDirective directive) : base($"{message} ({directive.FileName}: {directive.LineNumber})")
        {
            Error = message;
            Directive = directive;
        }

        /// <summary>
        /// Message explaining why the <see cref="Directive"/> or its configuration is invalid.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// The invalid <see cref="ScriptDirective"/>.
        /// </summary>
        public ScriptDirective Directive { get; }
    }
}
