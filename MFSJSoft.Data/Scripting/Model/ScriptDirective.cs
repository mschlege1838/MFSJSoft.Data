
using System;
using System.Collections.Generic;

namespace MFSJSoft.Data.Scripting.Model
{
    public class ScriptDirective : IFormattable
    {
        internal ScriptDirective(string name, IList<string> arguments, string fileName, int lineNumber)
        {
            Name = name;
            Arguments = arguments;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        public string Name { get; }
        public IList<string> Arguments { get; }
        public string FileName { get; }
        public int LineNumber { get; }

        public override string ToString()
        {
            return ToString("G", null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return format switch
            {
                "S" => $"{Name}: {string.Join(',', Arguments)}",
                _ => $"{Name}: {string.Join(',', Arguments)} ({FileName}: {LineNumber})"
            };
        }
    }

}

