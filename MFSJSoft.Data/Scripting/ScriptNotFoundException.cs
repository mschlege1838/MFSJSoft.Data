using System;

namespace MFSJSoft.Data.Scripting
{

    /// <summary>
    /// Thrown when a script could not be resolved.
    /// </summary>
    /// <remarks>
    /// Fallback exception thrown by <see cref="ScriptExecutor"/> when a chosen <see cref="IScriptResolver"/>
    /// returns <see langword="null" />.
    /// </remarks>
    public class ScriptNotFoundException : Exception
    {

        internal ScriptNotFoundException(string name) : base($"Requested script not found: {name}")
        {
            Name = name;
        }

        /// <summary>
        /// The <c>name</c> of the script, as passed to <see cref="ScriptExecutor.ExecuteScript"/>.
        /// </summary>
        public string Name { get; }
    }
}
