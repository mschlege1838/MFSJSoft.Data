
using System;
using System.Collections.Generic;

namespace MFSJSoft.Data.Scripting.Model
{

    /// <summary>
    /// Represents the contents of a declared script directive.
    /// </summary>
    public class ScriptDirective : IFormattable
    {
        internal ScriptDirective(string name, IList<string> arguments, string fileName, int lineNumber)
        {
            Name = name;
            Arguments = arguments;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// The name of the directive.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The directive's argument list.
        /// </summary>
        public IList<string> Arguments { get; }

        /// <summary>
        /// The name of the script file in which the directive is declared.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The first line number on which the directive is declared.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Returns the string representation of the directive along with the <see cref="FileName"/> and
        /// <see cref="LineNumber"/>. In standard format terms, returns the <c>G</c> (General) format of
        /// the directive.
        /// </summary>
        /// <returns>The general string representation of the directive.</returns>
        public override string ToString()
        {
            return ToString("G", null);
        }

        /// <summary>
        /// Returns either the <c>G</c> (General) representation of the directive, which includes the
        /// directive name, argument list, and <see cref="FileName"/>/<see cref="LineNumber"/>, or the
        /// <c>S</c> (Short) representation, which only includes the directive name and argument list.
        /// </summary>
        /// <param name="format">
        /// <c>S</c> for the short format described above, or <c>G</c> for the general format. Implementation
        /// note: currently an argument of anything other than <c>S</c> will return the general format, however
        /// using <c>G</c>, or calling <see cref="ToString()"/> is recommended.
        /// </param>
        /// <param name="formatProvider">Currently ignored; can be <c>null</c>.</param>
        /// <returns>The directive represented as a string, as described above.</returns>
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

