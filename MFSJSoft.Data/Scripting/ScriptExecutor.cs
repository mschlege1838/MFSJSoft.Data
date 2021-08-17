using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using MFSJSoft.Data.Scripting.Model;
using MFSJSoft.Data.Scripting.Processor;
using MFSJSoft.Data.Util;

namespace MFSJSoft.Data.Scripting
{

    /// <summary>
    /// <para>Core class for executing SQL scripts within applications. Provides convenient integration between 
    /// SQL script files and application code. This class will call on application code within the script 
    /// execution context as configured.</para>
    /// </summary>
    ///
    /// 
    /// <remarks>
    /// <para>The <c>ScriptExecutor</c> class provides a convenient integration between application code
    /// and SQL scripts. The processing model uses directives declared within SQL comments to control
    /// how scripts are executed.</para>
    /// 
    /// <para>Directives are specified in an SQL line or block comment. Line directives are specified 
    /// as follows:
    /// <code lang="sql">
    /// ---- #[DirectiveName]: [ArgumentList]
    /// </code>
    /// Block directives are specified as follows:
    /// <code lang="sql">
    /// /* ** #[DirectiveName]: [ArgumentList] */
    /// </code>
    /// Zero or more horizontal whitespace characters can appear where there are single spaces in the
    /// examples above. Note a line directive must end with a newline sequence (CR, LF, or CRLF), and block 
    /// directives can contain arbitrary newlines, but must end with the <c>*/</c> sequence.</para>
    /// 
    /// <para>Argument lists are comma separated values. Argument values that contain only alphanumeric 
    /// characters can be specified as-is. All other values must be specified as quoted strings.</para>
    /// 
    /// <para>Quoted strings are enclosed in single (<c>'</c>) or double (<c>"</c>) quotes. Any character preceded 
    /// with a backslash (<c>\</c>) in a quoted string will be escaped (emitted as-is) by the lexer. If two of the 
    /// same quotation characters used to open the string are found in tandum within the string, they
    /// will also be escaped as a single quote character.</para>
    /// 
    /// <para>Quoted strings cannot contain newline sequences. If newlines are required in a quoted 
    /// string value, use triple quotes to open and close the string (I.e. <c>"""</c> or <c>'''</c>). The same escape 
    /// rules above apply to triple-quoted string values.</para>
    /// 
    /// <para>Scripts are initialized as they are passed to the 
    /// <see cref="ScriptExecutor.ExecuteScript(string, IScriptProcessor, string)">ExecuteScript</see>
    /// method. After a script is initialized, its state is saved for subsequent execution; application restart is
    /// required to observe changes in the underlying script source code.</para>
    /// 
    /// <para>The default statement terminator is the semicolon (<c>;</c>), but can be
    /// any character sequence, so long as it does not contain characters used by <c>ScriptExecutor</c> to
    /// process directives. Statement terminators can be specified on a per-script basis to 
    /// <see cref="ScriptExecutor.ExecuteScript(string, IScriptProcessor, string)">ExecuteScript</see>, but are only
    /// used to compile the script the first time it is initalized.</para>
    /// 
    /// <para>Generally application code will use the <see cref="CompositeProcessor" /> for script execution, and pass 
    /// in relevant predefined <see cref="IDirectiveProcessor">IDirectiveProcessors</see> depending on directives used 
    /// within the script to be executed.</para>
    /// 
    /// <para>The predefined directives are summarized below:</para>
    /// <list type="bullet">
    /// <item>
    ///     <term><c>#Callback: statementName?</c></term>
    ///     <description>
    ///     Passes the statement text with the given <c>statementName</c> back to application code through an 
    ///     <see cref="CallbackProcessor.ExecuteStatement">ExecuteStatement</see> delegate registered with a
    ///     <see cref="CallbackProcessor" />. It is up to application code to create and execute an appropriate
    ///     <see cref="IDbCommand"/>.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><c>#ExecuteIf: propertyName, propertyValue?</c></term>
    ///     <description>
    ///     Conditionally executes the statement on which it is defined. With one argument, will test whether the value
    ///     of the given <c>propertyName</c> is defined, and not equal to (ignoring case) <c>false</c>. With two arguments,
    ///     will test whether the property is equal to the given <c>propertyValue</c>. If the test passes, the statement will
    ///     be executed, otherwise, it will be skipped. Properties are supplied in <see cref="IProperties"/> passed to the
    ///     <see cref="ExecuteIfProcessor" />.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><c>#If[Not]: propertyName, propertyValue?, ifTrueValue?, ifFalseValue?</c></term>
    ///     <description>
    ///     Conditionally inserts the given <c>ifTrueValue</c> in place of this directive in the statement text. With 
    ///     two arguments, the <c>ifTrueValue</c> will be inserted if the value of the given <c>propertyName</c> is defined 
    ///     and not equal to (ignoring case) <c>false</c>. With three arguments, the <c>ifTrueValue</c> will be inserted
    ///     if the property is equal to <c>propertyValue</c>. With four arguments, the given <c>ifFalseValue</c> will be 
    ///     inserted the property is not equal to <c>propertyValue</c>. If the asterick (<c>*</c>) is given as 
    ///     <c>propertyValue</c>, the "if defined, not <c>false</c>" evaluation performed with the two-argment form is
    ///     executed instead of a string-literal comparison. Properties are supplied in <see cref="IProperties"/> passed
    ///     to the <see cref="IfProcessor"/>.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><c>#If[Not]Def: propertyName, propertyValue?, ifTrueValue, ifFalseValue?</c></term>
    ///     <description>
    ///     Identical to the <c>If[Not]</c> directive above, except only executed once, when the script is first compiled.
    ///     Use this form for static properties that won't change over the lifetime of the application. Use the previous
    ///     form if evaluation is to be done based upon properties to be supplied at runtime.  Properties are supplied in 
    ///     <see cref="IProperties"/> passed to the <see cref="IfDefProcessor"/>
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><c>#LoadTable: tableName, createTemporary, columnDefs...</c></term>
    ///     <description>
    ///     
    ///     <para>Initializes a <see cref="DbBatchLoader"/>, based on the given <c>tableName</c> and <c>columnDefs</c> 
    ///     and passes back to a <see cref="LoadTableProcessor.WithLoader">WithLoader</see> callback with the given 
    ///     <c>tableName</c>. The <c>createTemporary</c> argument must be <c>true</c> or <c>false</c>. If <c>true</c>, 
    ///     a temporary table of the given <c>tableName</c> will be implicitly created. If <c>false</c>, the given 
    ///     <c>tableName</c> must exist within scope of the current database session.</para>
    ///     
    ///     <para>The <c>columnDefs</c> portion of this directive's argument list is variadic, and each value must be 
    ///     specified as a quoted string. Values are a comma-separated list of two, three, or four values:</para>
    ///     <list type="number">
    ///     <item>
    ///         <term><c>name</c> (Required)</term>
    ///         <description>
    ///         The target table column name. When translated to a parameter, the parameter name will be this with an at 
    ///         (<c>@</c>) character prepended.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><c>Type</c> (Required)</term>
    ///         <description>
    ///         The database type of the target column. Must be specified as a string-literal matching the name of one of
    ///         the <see cref="DbType" /> enumeration contstants.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><c>size</c> OR <c>precision</c> (Optional)</term>
    ///         <description>
    ///         In the 3-value form, the max size of the column type (typically for string data types). In the 3-value form,
    ///         the precision of a numeric data type.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term><c>scale</c></term>
    ///         <description>The scale for a numeric data type.</description>
    ///     </item>
    ///     </list>
    ///     
    ///     <para>Client code is responsible for assigning <see cref="DbBatchLoader.InputData">InputData</see> to the
    ///     <see cref="DbBatchLoader" /> and defining the <see cref="DbBatchLoader.RowDelegate" /> callback to update
    ///     <see cref="DataRow" /> instances with application data.</para>
    ///     
    ///     <para>Client code is also responsible for calling <see cref="DbBatchLoader.Execute" /> once all required
    ///     properties are defined. The directive will ultimately be processed through a <see cref="DbDataAdapter"/>
    ///     with its <see cref="DbDataAdapter.UpdateBatchSize"/> set to <c>0</c>. See the <see cref="DbBatchLoader" />
    ///     for further details.</para>
    ///     
    ///     <para>The default mapping between <see cref="DbType"/> constants and SQL data types used for implicit
    ///     temporary table creation follows semantics that are compatible with SQL Server, but strives to be as
    ///     ANSI-friendly as possible. Both these and the prefix used for creating temporary tables (which follows
    ///     SQL Server semantics only) can be overriden in global properties, and as optional arguments to the 
    ///     constructor of <see cref="LoadTableProcessor" /></para>
    ///     </description>
    /// </item>
    /// </list>
    /// </remarks>
    public class ScriptExecutor
    {

        internal static readonly RegexReplacer DirectivePlaceholderReplacer = new(new Regex(@"\{([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})\}"));

        readonly IScriptResolver resolver;
        readonly IDictionary<Type, object> processorConfig;
        readonly IDictionary<(string, object), IList<InitializedStatement>> compiledScripts = new Dictionary<(string, object), IList<InitializedStatement>>();


        public ScriptExecutor(IScriptResolver resolver = null, IDictionary<Type, object> processorConfig = null)
        {
            this.resolver = resolver;
            this.processorConfig = processorConfig;
        }

        public void ExecuteScript(string name, IScriptProcessor processor, string statementTerminator = null)
        {
            var processorType = processor.GetType();
            var scriptKey = (name, processor is IIdentifiable identifiable ? identifiable.Id : processorType);
            if (processorConfig is not null && processorConfig.ContainsKey(processorType))
            {
                processor.InitProcessor(processorConfig[processorType]);
            }
            else
            {
                processor.InitProcessor(null);
            }

            // Retrieve script.
            IList<InitializedStatement> script;
            if (compiledScripts.ContainsKey(scriptKey))
            {
                // Use cached, if present.
                script = compiledScripts[scriptKey];
            }
            else
            {
                // Compile script.
                script = compiledScripts[scriptKey] = new List<InitializedStatement>();

                // Obtain source.
                ScriptSource source;
                if (processor is IScriptResolver resolver)
                {
                    source = resolver.Resolve(name);
                }
                else if (this.resolver is not null)
                {
                    source = this.resolver.Resolve(name);
                }
                else
                {
                    source = new ScriptSource(File.ReadAllText(name), name, statementTerminator);
                }

                // Process Statements.
                foreach (var statement in source.Parse())
                {
                    var directiveState = new List<InitializedDirective>();
                    var deferredDirectives = new Dictionary<string, (InitializedDirective, int)>();
                    
                    var text = DirectivePlaceholderReplacer.Process(statement.Text, (m, buf) =>
                    {
                        var directiveId = m.Groups[1].Value;
                        var directive = statement.Directives[directiveId];

                        var initialization = processor.InitDirective(directive);
                        if ((initialization.Action & DirectiveInitializationAction.REPLACE_TEXT) == DirectiveInitializationAction.REPLACE_TEXT)
                        {
                            buf.Append(initialization.ReplacementText);
                        }

                        var initializedDirective = new InitializedDirective(directive, directiveId, initialization.InitializedState);

                        
                        var deferred = (initialization.Action & DirectiveInitializationAction.DEFER_SETUP) == DirectiveInitializationAction.DEFER_SETUP;
                        var noStore = (initialization.Action & DirectiveInitializationAction.NO_STORE) == DirectiveInitializationAction.NO_STORE;
                        
                        if (noStore && deferred)
                        {
                            throw new InvalidOperationException($"Processor requested deferred setup, but requested directive not be stored for subsequent execution: {directive}");
                        }

                        if (deferred)
                        {
                            deferredDirectives[directiveId] = (initializedDirective, directiveState.Count);
                            buf.Append('{').Append(directiveId).Append('}');
                        }

                        if (!noStore)
                        {
                            directiveState.Add(initializedDirective);
                        }
                    });

                    script.Add(new InitializedStatement(text, directiveState, deferredDirectives, statement.FileName, statement.LineNumber));
                }

            }
            

            // Execute Script.
            foreach (var statement in script)
            {
                (var text, var initializedDirectives) = statement.GetExecutionParams(processor);
                
                var directiveState = new List<(ScriptDirective, object)>();
                foreach (var initializedDirective in initializedDirectives)
                {
                    directiveState.Add((initializedDirective.Directive, initializedDirective.InitializedState));
                }

                try
                {
                    processor.ExecuteStatement(text, directiveState);
                }
                catch (Exception e)
                {
                    throw new StatementExecutionException(text, statement.FileName, statement.LineNumber, e);
                }
            }

        }
      
    }

    public class StatementExecutionException : Exception
    {
        public StatementExecutionException(string text, string fileName, int lineNumber, Exception rootCause) : base($"Error executing statement: {text} ({fileName}:{lineNumber})", rootCause)
        {
            Text = text;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        public string Text { get; }
        public string FileName { get; }
        public int LineNumber { get; }
    }


    class RegexReplacer
    {
        readonly Regex pattern;

        internal RegexReplacer(Regex pattern)
        {
            this.pattern = pattern;
        }

        internal delegate void OnMatch(Match m, StringBuilder buf);

        internal string Process(string text, OnMatch onMatch)
        {
            var startIndex = 0;
            var buf = new StringBuilder();
            while (true)
            {
                var m = pattern.Match(text, startIndex);
                if (!m.Success)
                {
                    break;
                }

                buf.Append(text[startIndex..m.Index]);
                startIndex = m.Index + m.Length;

                onMatch(m, buf);
            }

            if (startIndex < text.Length)
            {
                buf.Append(text[startIndex..]);
            }

            return buf.ToString();
        }
    }


    class InitializedStatement
    {
        internal InitializedStatement(string text, IList<InitializedDirective> directives, IDictionary<string, (InitializedDirective, int)> deferredDirectives, string fileName, int lineNumber)
        {
            Text = text;
            Directives = directives;
            DeferredDirectives = deferredDirectives;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        internal string Text { get; }
        internal IList<InitializedDirective> Directives { get; }
        internal IDictionary<string, (InitializedDirective, int)> DeferredDirectives { get; }
        internal string FileName { get; }
        internal int LineNumber { get; }

        internal (string, IList<InitializedDirective>) GetExecutionParams(IScriptProcessor processor)
        {
            if (DeferredDirectives.Count == 0)
            {
                return (Text, Directives);
            }
            else
            {
                var setupDirectives = new List<InitializedDirective>(Directives);
                var text = ScriptExecutor.DirectivePlaceholderReplacer.Process(Text, (m, buf) =>
                {
                    var directiveId = m.Groups[1].Value;
                    (var initState, var targetIndex) = DeferredDirectives[directiveId];

                    var initialization = processor.SetupDirective(initState.Directive, initState.InitializedState);
                    if (initialization is null)
                    {
                        throw new InvalidOperationException();
                    }

                    if ((initialization.Action & DirectiveInitializationAction.REPLACE_TEXT) == DirectiveInitializationAction.REPLACE_TEXT)
                    {
                        buf.Append(initialization.ReplacementText);
                    }

                    if ((initialization.Action & DirectiveInitializationAction.NO_STORE) == DirectiveInitializationAction.NO_STORE)
                    {
                        setupDirectives.RemoveAt(targetIndex);
                    }
                    else
                    {
                        setupDirectives[targetIndex] = new InitializedDirective(initState.Directive, directiveId, initialization.InitializedState);
                    }

                });

                return (text, setupDirectives);
            }
        }
    }

    class InitializedDirective
    {
        internal InitializedDirective(ScriptDirective directive, string directiveId, object initializedState)
        {
            Directive = directive;
            DirectiveId = directiveId;
            InitializedState = initializedState;
        }

        internal ScriptDirective Directive { get; }
        internal string DirectiveId { get; }
        internal object InitializedState { get; }
    }
}
