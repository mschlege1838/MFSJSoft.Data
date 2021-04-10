
using System.IO;

namespace MFSJSoft.Data.Scripting
{
    public class BaseDirScriptResolver : IScriptResolver
    {

        readonly string baseDir;
        readonly string statementTerminator;

        public BaseDirScriptResolver(string baseDir = null, string statementTerminator = null)
        {
            this.baseDir = baseDir ?? Directory.GetCurrentDirectory();
            this.statementTerminator = statementTerminator;
        }

        public ScriptSource Resolve(string name)
        {
            var path = Path.Join(baseDir, name.Replace('/', Path.DirectorySeparatorChar));
            var fileName = Path.GetFileName(path);

            return new ScriptSource(File.ReadAllText(path), fileName, statementTerminator);
        }
    }
}
