using MFSJSoft.Data.Scripting.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;


namespace MFSJSoft.Data.Scripting.Processor
{

    public class CompositeProcessorContext
    {
        internal CompositeProcessorContext(DbConnection connection, DbTransaction transaction, bool noTimeout)
        {
            Connection = connection;
            Transaction = transaction;
            NoTimeout = noTimeout;
        }

        public DbProviderFactory ProviderFactory { get; internal set; }
        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; }
        public bool NoTimeout { get; }

        public DbCommand NewCommand()
        {
            var result = ProviderFactory.CreateCommand();
            result.Connection = Connection;
            result.Transaction = Transaction;
            if (NoTimeout)
            {
                result.CommandTimeout = 0;
            }
            return result;
        }
    }

    public class CompositeProcessorConfiguration
    {
        public DbProviderFactory ProviderFactory { get; set; }
        public IDictionary<Type, object> DirectiveConfiguration { get; set; }
    }

    public class CompositeProcessor : IScriptProcessor
    {

        readonly CompositeProcessorContext context;
        readonly ICollection<IDirectiveProcessor> processors;

        public CompositeProcessor(DbProviderFactory providerFactory, DbConnection connection, DbTransaction transaction, bool noTimeout, params IDirectiveProcessor[] processors)
        {
            context = new CompositeProcessorContext(connection ?? throw new ArgumentNullException(nameof(connection)), transaction, noTimeout) { ProviderFactory = providerFactory };
            this.processors = processors;
        }

        public CompositeProcessor(DbProviderFactory providerFactory, DbConnection connection, bool noTimeout, params IDirectiveProcessor[] processors) : this(providerFactory, connection, null, noTimeout, processors)
        {
            
        }

        public CompositeProcessor(DbProviderFactory providerFactory, DbConnection connection, DbTransaction transaction, params IDirectiveProcessor[] processors) : this(providerFactory, connection, transaction, false, processors)
        {
            
        }

        public CompositeProcessor(DbProviderFactory providerFactory, DbConnection connection, params IDirectiveProcessor[] processors) : this(providerFactory, connection, null, false, processors)
        {
            
        }

        public CompositeProcessor(DbConnection connection, bool noTimeout, params IDirectiveProcessor[] processors) : this(null, connection, null, noTimeout, processors)
        {

        }

        public CompositeProcessor(DbConnection connection, DbTransaction transaction, params IDirectiveProcessor[] processors) : this(null, connection, transaction, false, processors)
        {

        }

        public CompositeProcessor(DbConnection connection, params IDirectiveProcessor[] processors) : this(null, connection, null, false, processors)
        {

        }

        public void InitProcessor(object configuration)
        {
            if (configuration is not null && configuration is CompositeProcessorConfiguration lcfg)
            {
                if (context.ProviderFactory is null && lcfg.ProviderFactory is not null)
                {
                    context.ProviderFactory = lcfg.ProviderFactory;
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
