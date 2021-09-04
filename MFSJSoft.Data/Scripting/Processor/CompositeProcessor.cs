using MFSJSoft.Data.Scripting.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;

using Microsoft.Extensions.Logging;

namespace MFSJSoft.Data.Scripting.Processor
{

    public class CompositeProcessorContext
    {
        internal CompositeProcessorContext(DbConnection connection, DbTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        public DbProviderFactory ProviderFactory { get; internal set; }
        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; }
        public int CommandTimeout { get; internal set; }
        public ILogger Logger { get; internal set; }

        public DbCommand NewCommand()
        {
            var result = ProviderFactory.CreateCommand();
            result.Connection = Connection;
            result.Transaction = Transaction;
            result.CommandTimeout = CommandTimeout;
            return result;
        }
    }

    public class CompositeProcessorConfiguration
    {
        public DbProviderFactory ProviderFactory { get; set; }
        public int CommandTimeout { get; set; } = CompositeProcessor.DefaultTimeout;
        public IDictionary<Type, object> DirectiveConfiguration { get; set; }
    }

    public class CompositeProcessor : IScriptProcessor
    {

        public const int DefaultTimeout = 30;

        readonly CompositeProcessorContext context;
        readonly ICollection<IDirectiveProcessor> processors;
        int commandTimeout = DefaultTimeout;

        public CompositeProcessor(DbProviderFactory providerFactory, DbConnection connection, DbTransaction transaction, params IDirectiveProcessor[] processors)
        {
            context = new CompositeProcessorContext(connection ?? throw new ArgumentNullException(nameof(connection)), transaction) { ProviderFactory = providerFactory };
            this.processors = processors;
        }

        public CompositeProcessor(DbProviderFactory providerFactory, DbConnection connection, params IDirectiveProcessor[] processors) : this(providerFactory, connection, null, processors)
        {
            
        }

        public CompositeProcessor(DbConnection connection, DbTransaction transaction, params IDirectiveProcessor[] processors) : this(null, connection, transaction, processors)
        {

        }

        public CompositeProcessor(DbConnection connection, params IDirectiveProcessor[] processors) : this(null, connection, null, processors)
        {

        }

        public int CommandTimeout
        {
            get => commandTimeout;
            set => context.CommandTimeout = commandTimeout = value;
        }

        public void InitProcessor(object configuration, ILogger logger)
        {
            if (configuration is not null && configuration is CompositeProcessorConfiguration lcfg)
            {
                if (context.ProviderFactory is null && lcfg.ProviderFactory is not null)
                {
                    context.ProviderFactory = lcfg.ProviderFactory;
                }

                context.CommandTimeout = lcfg.CommandTimeout;
                if (commandTimeout != DefaultTimeout)
                {
                    context.CommandTimeout = commandTimeout;
                }

                foreach (var processor in processors)
                {
                    var processorType = processor.GetType();
                    if (lcfg.DirectiveConfiguration is not null && lcfg.DirectiveConfiguration.ContainsKey(processorType))
                    {
                        processor.InitProcessor(lcfg.DirectiveConfiguration[processorType]);
                    }
                    else
                    {
                        processor.InitProcessor(null);
                    }
                }

            }

            context.Logger = logger;
            
            if (context.ProviderFactory is null)
            {
                throw new NullReferenceException($"Provider Factory must either be provided in a constructor of {typeof(CompositeProcessorContext)}, or in the applicable global configuration of parent {typeof(ScriptExecutor)}.");
            }
        }

        public DirectiveInitialization InitDirective(ScriptDirective directive)
        {
            foreach (var processor in processors)
            {

                DirectiveInitialization initialization;
                try
                {
                    initialization = processor.InitDirective(context, directive);
                }
                catch (UnrecognizedDirectiveException)
                {
                    initialization = null;
                }

                if (initialization is not null)
                {
                    return initialization;
                }
            }

            throw new UnrecognizedDirectiveException(directive);
        }

        public DirectiveInitialization SetupDirective(ScriptDirective directive, object initState)
        {
            foreach (var processor in processors)
            {
                DirectiveInitialization initialization;
                try
                {
                    initialization = processor.SetupDirective(context, directive, initState);
                }
                catch (Exception e) when (e is NotImplementedException || e is UnrecognizedDirectiveException)
                {
                    initialization = null;
                }

                if (initialization is not null)
                {
                    return initialization;
                }
            }

            throw new InvalidDirectiveException($"Processor requested directive setup, but no matching processor found: {directive:S}", directive);
        }

        public void ExecuteStatement(string text, IList<(ScriptDirective, object)> directives)
        {
            var executed = false;
            foreach ((var directive, var initState) in directives)
            {
                foreach (var processor in processors)
                {
                    if (processor.TryExecute(context, text, directive, initState) && !executed)
                    {
                        executed = true;
                    }
                }
            }

            if (!executed)
            {
                using var command = context.NewCommand();
                command.CommandText = text;
                command.ExecuteNonQuery();
            }
        }

        
    }
}
