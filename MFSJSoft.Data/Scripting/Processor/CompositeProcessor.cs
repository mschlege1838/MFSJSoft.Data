using MFSJSoft.Data.Scripting.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;


namespace MFSJSoft.Data.Scripting.Processor
{

    public class CompositeProcessorContext
    {
        internal CompositeProcessorContext(DbProviderFactory providerFactory, DbConnection connection, DbTransaction transaction, bool noTimeout)
        {
            ProviderFactory = providerFactory;
            Connection = connection;
            Transaction = transaction;
            NoTimeout = noTimeout;
        }

        public DbProviderFactory ProviderFactory { get; }
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

    public class CompositeProcessor : IScriptProcessor
    {

        readonly CompositeProcessorContext context;
        readonly ICollection<IDirectiveProcessor> processors;

        public CompositeProcessor(DbProviderFactory providerFactory, DbConnection connection, DbTransaction transaction, bool noTimeout, params IDirectiveProcessor[] processors)
        {
            context = new CompositeProcessorContext(providerFactory ?? throw new ArgumentNullException(nameof(providerFactory)), connection ?? throw new ArgumentNullException(nameof(connection)), transaction, noTimeout);
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
