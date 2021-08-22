namespace MFSJSoft.Data.Scripting
{

    /// <summary>
    /// Accessor interface for resolving SQL scripts.
    /// </summary>
    /// <remarks>
    /// The <c>name</c> is given as passed to <see cref="ScriptExecutor.ExecuteScript"/>. Generally these should be set up
    /// to resolve contextual segments of <c>name</c> (e.g. its directory path) using a common character for portability. In
    /// most cases, this should be the forward slash (<c>/</c>).
    /// </remarks>
    public interface IScriptResolver
    {

        /// <summary>
        /// Return a <see cref="ScriptSource"/> containing information for the script of the given <c>name</c>, or
        /// <see langword="null" /> if no script of the given <c>name</c> could be found. A more approperite exception
        /// (e.g. <see cref="System.IO.FileNotFoundException"/>) can also be thrown in this case.
        /// </summary>
        /// <param name="name">Name of the script to be resolved.</param>
        /// <returns><see cref="ScriptSource"/> containing information for the script of the given <c>name</c>, or
        /// <see langword="null" /> if no script of the given <c>name</c> could be found.</returns>
        ScriptSource Resolve(string name);

    }
}
