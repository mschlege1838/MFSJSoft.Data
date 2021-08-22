
using System.IO;

namespace MFSJSoft.Data.Scripting
{

    /// <summary>
    /// A simple <see cref="IScriptResolver"/> that resolves scripts from the given base directory.
    /// </summary>
    /// <remarks>
    /// If no base directory is specified, the application's <see cref="Directory.GetCurrentDirectory">current directory</see> is
    /// used. A custom statement terminator can also be specified.
    /// </remarks>
    public class BaseDirScriptResolver : IScriptResolver
    {

        readonly string baseDir;
        readonly string statementTerminator;

        /// <summary>
        /// Construct a new <see cref="BaseDirScriptResolver"/> with the given <c>baseDir</c> and <c>statementTerminator</c>.
        /// </summary>
        /// <param name="baseDir">Base directory from which to resolve scripts. If <see langword="null" />, will be substituted
        /// with <see cref="Directory.GetCurrentDirectory"/>.</param>
        /// <param name="statementTerminator">A custom statement terminator used in scripts resolved by this
        /// <see cref="IScriptResolver"/>.</param>
        public BaseDirScriptResolver(string baseDir = null, string statementTerminator = null)
        {
            this.baseDir = baseDir ?? Directory.GetCurrentDirectory();
            this.statementTerminator = statementTerminator;
        }

        /// <summary>
        /// Resolve the script of the given name against the file system, using a path constructed from the <c>baseDir</c> with which
        /// the <see cref="BaseDirScriptResolver"/> was constructed.
        /// </summary>
        /// <remarks>
        /// All forward slash/solidus (<c>/</c>) characters in <c>name</c> are converted to the local <see cref="Path.DirectorySeparatorChar"/>.
        /// </remarks>
        /// <param name="name">Script name, as passed to <see cref="ScriptExecutor.ExecuteScript"/>.</param>
        /// <returns>A <see cref="ScriptSource"/> containing information for the requested script.</returns>
        /// <exception cref="FileNotFoundException">If <c>name</c> cannot be resolved in the file system within the base directory as
        /// passed to the constructor.</exception>
        public ScriptSource Resolve(string name)
        {
            var path = Path.Join(baseDir, name.Replace('/', Path.DirectorySeparatorChar));
            var fileName = Path.GetFileName(path);

            return new ScriptSource(File.ReadAllText(path), fileName, statementTerminator);
        }
    }
}