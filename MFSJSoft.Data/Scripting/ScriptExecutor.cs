using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting
{

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

        internal string Text { get;}
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

    
}
