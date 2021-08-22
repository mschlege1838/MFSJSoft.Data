using System;

using MFSJSoft.Data.Scripting.Model;
using MFSJSoft.Data.Scripting.Processor;

namespace MFSJSoft.Data.Scripting
{

    /// <summary>
    /// Thrown by the <see cref="IScriptProcessor">IScriptProcessors</see> when they encounter an unrecognized directive.
    /// </summary>
    /// <remarks>
    /// Will be thrown by the <see cref="CompositeProcessor"/> when it encounters a directive for which no matching
    /// <see cref="IDirectiveProcessor"/> is found.
    /// </remarks>
    public class UnrecognizedDirectiveException : Exception
    {
        
        /// <summary>
        /// Constructs an <see cref="UnrecognizedDirectiveException"/> with the given <see cref="ScriptDirective"/>.
        /// </summary>
        /// <param name="directive">The unrecognized script directive.</param>
        public UnrecognizedDirectiveException(ScriptDirective directive) : base($"Unrecognized directive: {directive}")
        {
            Directive = directive;
        }

        /// <summary>
        /// <see cref="ScriptDirective"/> representing the directive in the script that is not recognized.
        /// </summary>
        public ScriptDirective Directive { get; }
    }
}
