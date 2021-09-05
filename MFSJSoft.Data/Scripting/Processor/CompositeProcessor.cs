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
        public IDictionary<object, object> DirectiveConfiguration { get; set; }
    }


    /// <summary>
    /// <see cref="IScriptProcessor"/> that processes scripts by delegating to individual <see cref="IDirectiveProcessor">IDirectiveProcessors</see>,
    /// while introducting some context common to database application programming.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>Generally, application code will use the predefined <see cref="IDirectiveProcessor">IDirectiveProcessors</see>, but the manner in which 
    /// <see cref="IDirectiveProcessor">IDirectiveProcessors</see> are executed is defined below. The description assumes famailiartiy with how 
    /// <see cref="ScriptExecutor"/> executes <see cref="IScriptProcessor">IScriptProcessors</see>, as this class simply delegates to a collection of
    /// <see cref="IDirectiveProcessor">IDirectiveProcessors</see> to enable compositing individual script directive handlers, rather than having to resort
    /// to one monolithic <see cref="IScriptProcessor"/>.</para>
    /// 
    /// <para>For more information on the predefined directive handlers, and examples of their usage with <see cref="CompositeProcessor"/>, please see
    /// their documentation pages:</para>
    /// <list type="bullet">
    ///     <item><see cref="CallbackProcessor"/></item>
    ///     <item><see cref="ExecuteIfProcessor"/></item>
    ///     <item><see cref="IfDefProcessor"/></item>
    ///     <item><see cref="IfProcessor"/></item>
    ///     <item><see cref="LoadTableProcessor"/></item>
    /// </list>
    /// 
    /// <para>For each directive encountered in a script, this class iterates its collection of <see cref="IDirectiveProcessor">IDirectiveProcessors</see>, calling
    /// each of the respective methods common to <see cref="IScriptProcessor"/>.</para>
    /// 
    /// <para>For <see cref="IDirectiveProcessor.InitDirective"/> and <see cref="IDirectiveProcessor.SetupDirective"/>, the first non-<see langword="null"/>
    /// <see cref="DirectiveInitialization"/> successsfully returned from the <see cref="IDirectiveProcessor"/> is returned from this class's respective 
    /// <see cref="IScriptProcessor"/> method. If <see cref="IDirectiveProcessor.InitDirective"/> or <see cref="IDirectiveProcessor.SetupDirective"/> throws an 
    /// <see cref="UnrecognizedDirectiveException"/>, it is treated identically to a <see langword="null"/> return value. If no <see cref="DirectiveInitialization"/>
    /// is successfully returned from any <see cref="IDirectiveProcessor"/>, an <see cref="UnrecognizedDirectiveException"/> is thrown up to the parent
    /// <see cref="ScriptExecutor"/>.</para>
    /// 
    /// <para>The <see cref="IDirectiveProcessor.TryExecute"/> method is called for each <see cref="IDirectiveProcessor"/> for each statement in the script.
    /// If none returns <see langword="true"/>, a generic <see cref="DbCommand"/> is <see cref="CompositeProcessorContext">created</see> using the given 
    /// <see cref="DbProviderFactory"/>, and is <see cref="DbCommand.ExecuteNonQuery">executed</see> as a non-query.</para>
    /// 
    /// </remarks>
    public class CompositeProcessor : IScriptProcessor
    {

        /// <summary>
        /// Default timeout for generic <see cref="DbCommand">DbCommands</see> <see cref="CompositeProcessorContext">created</see> within the
        /// context of a <see cref="CompositeProcessor"/>.
        /// </summary>
        /// <remarks>
        /// Consistent with SQL Server, the default value is 30s.
        /// </remarks>
        public const int DefaultTimeout = 30;

        readonly CompositeProcessorContext context;
        readonly ICollection<IDirectiveProcessor> processors;
        int commandTimeout = DefaultTimeout;

        /// <summary>
        /// Primary constructor.
        /// </summary>
        /// <param name="providerFactory"><see cref="DbProviderFactory"/> to use for the creation of generic <see cref="DbCommand"/>, <see cref="DbParameter"/>,
        /// etc. instances.</param>
        /// <param name="connection"><see cref="DbConnection"/> on which to execute <see cref="DbCommand">DbCommands</see>.</param>
        /// <param name="transaction">Current <see cref="DbTransaction"/> (if any).</param>
        /// <param name="processors">Composite sequence of <see cref="IDirectiveProcessor"/> instances used to execute the script.</param>
        public CompositeProcessor(DbProviderFactory providerFactory, DbConnection connection, DbTransaction transaction, params IDirectiveProcessor[] processors)
        {
            context = new CompositeProcessorContext(connection ?? throw new ArgumentNullException(nameof(connection)), transaction) { ProviderFactory = providerFactory };
            this.processors = processors;
        }

        /// <summary>
        /// No-transaction constructor.
        /// </summary>
        /// <param name="providerFactory"></param>
        /// <param name="connection"></param>
        /// <param name="processors"></param>
        public CompositeProcessor(DbProviderFactory providerFactory, DbConnection connection, params IDirectiveProcessor[] processors) : this(providerFactory, connection, null, processors)
        {
            
        }

        /// <summary>
        /// Constructor omitting the <see cref="DbProviderFactory"/>, assuming it is registered with the global config passed to the constructor
        /// of the parent <see cref="ScriptExecutor(IScriptResolver, IDictionary{object, object}, ILogger)"/>.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="processors"></param>
        public CompositeProcessor(DbConnection connection, DbTransaction transaction, params IDirectiveProcessor[] processors) : this(null, connection, transaction, processors)
        {

        }

        /// <summary>
        /// No-transaction version of the <see cref="DbProviderFactory"/>-omitting <see cref="CompositeProcessor(DbConnection, DbTransaction, IDirectiveProcessor[])">constructor</see>.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="processors"></param>
        public CompositeProcessor(DbConnection connection, params IDirectiveProcessor[] processors) : this(null, connection, null, processors)
        {

        }

        /// <summary>
        /// Instance-level override for the <see cref="DbCommand.CommandTimeout">timeout</see>
        /// </summary>
        public int CommandTimeout
        {
            get => commandTimeout;
            set => context.CommandTimeout = commandTimeout = value;
        }

        /// <summary>
        /// Analog method for <see cref="IScriptProcessor.InitDirective" />. The <see cref="IDirectiveProcessor.InitDirective"/>
        /// method is called for each processor given in the constructor.
        /// </summary>
        /// <remarks>
        /// <para>As the <see cref="CompositeProcessor"/> is a <see cref="IScriptProcessor"/>, the global configuration object is
        /// the value keyed under the <see cref="CompositeProcessor"/> type in the global configuration given to the constructor
        /// of <see cref="ScriptExecutor"/>.</para>
        /// 
        /// <para>Global configuration values for individual <see cref="IDirectiveProcessor">IDirectiveProcessors</see>
        /// should be given in the <see cref="CompositeProcessorConfiguration.DirectiveConfiguration"/> property of the global
        /// configuration for <see cref="CompositeProcessor"/>.</para>
        /// 
        /// <para>As with how distinct <see cref="IScriptProcessor"/> types are identified in <see cref="ScriptExecutor"/>, if
        /// an <see cref="IDirectiveProcessor"/> implements <see cref="IIdentifiable"/>, its <see cref="IIdentifiable.Id"/> property
        /// will be used to identify it, otherwise its runtime <see cref="object.GetType">type</see>.</para>
        /// </remarks>
        /// <param name="configuration">Global configuration object applicable to <see cref="CompositeProcessor"/>.</param>
        /// <param name="logger">Global <see cref="ILogger"/> associated with the parent <see cref="ScriptExecutor"/>.</param>
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
                    var processorKey = (processor is IIdentifiable lp) ? lp.Id : processor.GetType();
                    if (lcfg.DirectiveConfiguration is not null && lcfg.DirectiveConfiguration.ContainsKey(processorKey))
                    {
                        processor.InitProcessor(lcfg.DirectiveConfiguration[processorKey]);
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
