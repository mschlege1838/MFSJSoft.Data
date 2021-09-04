using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Xml;

using Microsoft.Extensions.Logging;

namespace MFSJSoft.Data.Util
{

    /// <summary>
    /// Generic database batch loader for relational, table-structured data. An ADO <see cref="DataAdapter"/> is
    /// used to perform batch operations.
    /// </summary>
    /// <remarks>
    /// <param>The <see cref="DbBatchLoader"/> uses the provided <see cref="Connection">Connection</see> and 
    /// <see cref="Transaction">Transaction</see> (if any) to perform batch data loads in a generic, well-defined
    /// manner.</param>
    /// 
    /// <para>Although all properties are readable and writable for convenience, the following are required at the time 
    /// <see cref="Execute">Execute</see> is called:</para>
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="Connection">Connection</see></term>
    ///         <description>Database connection, as would be assigned to <see cref="DbCommand"/> instances.</description>    
    ///     </item>
    ///     <item>
    ///         <term><see cref="SelectStatement">SelectStatement</see></term>
    ///         <description>A select statement used to define table metadata. The <see cref="DbDataAdapter.FillSchema(DataTable, SchemaType)"/>
    ///         method is used to define the <see cref="DataTable"/> schema (with <see cref="SchemaType.Source"/>); if the select
    ///         statement would otherwise return rows, they are ignored.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="InsertStatement">InsertStatement</see></term>
    ///         <description>Parameterized, single-row insert statement with parameters matching the names of those in the
    ///         <see cref="Parameters">Parameters</see> property.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Parameters">Parameters</see></term>
    ///         <description><see cref="IEnumerable{T}"/> of <see cref="DbParameter"/> instances configured for batch
    ///         inserts. I.e. the <see cref="DbParameter.ParameterName">ParameterName</see>, <see cref="DbParameter.DbType">DbType</see>,
    ///         <see cref="DbParameter.SourceColumn">SourceColumn</see>, and optionally the <see cref="DbParameter.Size">Size</see>,
    ///         <see cref="DbParameter.Precision">Precision</see>, and/or <see cref="DbParameter.Scale">Scale</see> properties
    ///         defined, and all others left <c>default</c>.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="InputData">InputData</see></term>
    ///         <description>Raw <see cref="System.Collections.IEnumerable">enumeration</see> of data to be loaded. Due to
    ///         constraints that would otherwise be required for generic typing, it is more convenient to make the appropriate 
    ///         casts from <see cref="object"/> in <see cref="RowDelegate">RowDelegate</see>.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="RowDelegate">RowDelegate</see></term>
    ///         <description>Callback function to perform row-by-row assignment from the raw <see cref="InputData">InputData</see>
    ///         to <see cref="DataRow"/> instances used in the batch.</description>
    ///     </item>
    /// </list>
    /// 
    /// <param>The remaining properties are optional:</param>
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="Transaction">Transaction</see></term>
    ///         <description>Assigned to the <see cref="DbCommand.Transaction"/> property for all commands created and
    ///         executed.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Logger">Logger</see></term>
    ///         <description>Defaults to <see cref="Console.Error"/>. Only warning information is logged; any error is thrown
    ///         as an approperite <see cref="Exception"/>.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CommandTimeout">CommandTimeout</see></term>
    ///         <description>Assigned to the <see cref="DbCommand.CommandTimeout"/> property for all commands created
    ///         and executed. Consistent with SQL Server, the default is <c>30</c>s.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="UpdateBatchSize">UpdateBatchSize</see></term>
    ///         <description>Assigned to the <see cref="DbDataAdapter.UpdateBatchSize"/> property for batch execution.
    ///         The default is <c>0</c> (i.e. no limit).</description>
    ///     </item>
    /// </list>
    /// 
    /// <para>The <see cref="Execute">Execute</see> method follows this sequence:</para>
    /// <list type="number">
    ///     <item>
    ///         <description>If <see cref="CreateStatement">CreateStatement</see> is not <see langword="null"/>, it will
    ///         be executed first. This is generally used to create a target temporary table for the load.</description>
    ///     </item>
    ///     <item>
    ///         <description>The <see cref="RowDelegate">RowDelegate</see> is called for each item in 
    ///         <see cref="InputData">InputData</see>.</description>
    ///     </item>
    ///     <item>
    ///         <description>After <see cref="InputData">InputData</see> is fully iterated, rows will be inserted using 
    ///         <see cref="InsertStatement">InsertStatement</see> parameterized with
    ///         <see cref="Parameters">Parameters</see>. If the <see cref="DbProviderFactory"/> supports the
    ///         <see cref="DbProviderFactory.CanCreateDataAdapter">creation</see> of <see cref="DbDataAdapter"/>
    ///         instances, the update is performed using <see cref="DbDataAdapter.Update(DataTable)"/>, otherwise a
    ///         warning is logged, and the update is performed sequentially with individual
    ///         <see cref="DbCommand">DbCommands</see>.</description>
    ///     </item>
    /// </list>
    /// </remarks>
    public class DbBatchLoader
    {

        /// <summary>
        /// A <c>delegate</c> called in <see cref="Execute">Execute</see> to update a <see cref="DataRow"/> for each item 
        /// in <see cref="InputData">InputData</see>.
        /// </summary>
        /// <param name="row"><see cref="DataRow"/> to update.</param>
        /// <param name="item">Item from <see cref="InputData">InputData</see> with which <c>row</c> is to be updated.</param>
        public delegate void UpdateRow(DataRow row, object item);

        readonly DbProviderFactory providerFactory;

        /// <summary>
        /// Construct a new instance of <see cref="DbBatchLoader"/>.
        /// </summary>
        /// <param name="providerFactory"><see cref="DbProviderFactory"/> used for creating <see cref="DbCommand"/>,
        /// <see cref="DbDataAdapter"/>, <see cref="DbParameter"/>, etc. instances.</param>
        /// <param name="connection"><see cref="DbConnection"/> on which to execute the batch.</param>
        /// <param name="transaction">Current <see cref="DbTransaction"/>, if any.</param>
        /// <param name="logger">Optional <see cref="ILogger"/> used to log warning information.</param>
        public DbBatchLoader(DbProviderFactory providerFactory, DbConnection connection = null, DbTransaction transaction = null, ILogger logger = null)
        {
            this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            Connection = connection;
            Transaction = transaction;
            Logger = logger;
        }

        /// <summary>
        /// <see cref="DbConnection"/> on which to execute the batch.
        /// </summary>
        public DbConnection Connection { get; set; }
        
        /// <summary>
        /// Current <see cref="DbTransaction"/>, if any.
        /// </summary>
        public DbTransaction Transaction { get; set; }
        
        /// <summary>
        /// <see cref="ILogger"/> used to log warning information. If <see langword="null"/>, warnings will be
        /// logged to <see cref="Console.Error"/>.
        /// </summary>
        public ILogger Logger { get; set; }


        /// <summary>
        /// Copied to the <see cref="DbCommand.CommandTimeout"/> property for all <see cref="DbCommand"/> instances
        /// created by this <see cref="DbDataAdapter"/>. Default is 30s.
        /// </summary>
        public int CommandTimeout { get; set; } = 30;
        /// <summary>
        /// Copied to the <see cref="DbDataAdapter.UpdateBatchSize"/> property on the <see cref="DbDataAdapter"/>
        /// used to execute the batch. Default is 0 (no limit).
        /// </summary>
        public int UpdateBatchSize { get; set; } = 0;


        /// <summary>
        /// Optional statement to create the target table. The target table is typically temporary. If
        /// <see langword="null"/>, this property will be ignored.
        /// </summary>
        public string CreateStatement { get; set; }
        
        /// <summary>
        /// Select statement used to <see cref="DbDataAdapter.FillSchema(DataTable, SchemaType)">Fill</see> the
        /// schema of the <see cref="DataTable"/> used to perform the update.
        /// </summary>
        /// <remarks>
        /// Only strictly required if the <see cref="DbProviderFactory"/>
        /// <see cref="DbProviderFactory.CanCreateDataAdapter">supports</see> <see cref="DbDataAdapter">DbDataAdapters</see>, 
        /// but should generally always be provided for portability.
        /// </remarks>
        public string SelectStatement { get; set; }
        /// <summary>
        /// Insert statement with parameters matching <see cref="Parameters">Parameters</see> used to execute the
        /// batch.
        /// </summary>
        public string InsertStatement { get; set; }

        /// <summary>
        /// <see cref="IEnumerable{T}">Enumeration</see> of <see cref="DbParameter"/> instances matching those defined in 
        /// <see cref="InsertStatement">InsertStatement</see>.
        /// </summary>
        public IEnumerable<DbParameter> Parameters { get; set; }
        
        /// <summary>
        /// Raw <see cref="System.Collections.IEnumerable">enumeration</see> of the desired input data.
        /// </summary>
        public System.Collections.IEnumerable InputData { get; set; }
        
        /// <summary>
        /// Delegate implementation <see cref="UpdateRow"/> used to update <see cref="DataRow"/> instances for each
        /// item in <see cref="Parameters">Parameters</see>.
        /// </summary>
        public UpdateRow RowDelegate { get; set; }
        

        /// <summary>
        /// Executes this batch update in the sequence described in this <see cref="DbBatchLoader">class-level</see>
        /// documentation.
        /// </summary>
        public void Execute()
        {
            if (RowDelegate is null)
            {
                throw new ArgumentNullException(nameof(RowDelegate));
            }
            if (InputData is null)
            {
                throw new ArgumentNullException(nameof(InputData));
            }
            if (InsertStatement is null)
            {
                throw new ArgumentNullException(nameof(InsertStatement));
            }
            if (Parameters is null)
            {
                throw new ArgumentNullException(nameof(Parameters));
            }



            void ProcessRows(DataTable table)
            {
                foreach (var item in InputData)
                {
                    var row = table.NewRow();
                    RowDelegate(row, item);
                    table.Rows.Add(row);
                }
            }

            if (CreateStatement is not null)
            {
                using var command = CreateCommand(CreateStatement);
                command.ExecuteNonQuery();
            }

            if (providerFactory.CanCreateDataAdapter)
            {

                using var selectCommand = CreateCommand(SelectStatement ?? throw new ArgumentNullException(nameof(SelectStatement)));
                using var insertCommand = CreateCommand(InsertStatement);

                var adapter = providerFactory.CreateDataAdapter();
                adapter.SelectCommand = selectCommand;
                adapter.InsertCommand = insertCommand;
                adapter.UpdateBatchSize = UpdateBatchSize;
                foreach (var parameter in Parameters)
                {
                    adapter.InsertCommand.Parameters.Add(parameter);
                }

                var table = new DataTable();
                adapter.FillSchema(table, SchemaType.Source);

                ProcessRows(table);

                adapter.Update(table);
            }
            else
            {
                if (Logger is not null)
                {
                    Logger.LogWarning("Attempting to batch load data, however registered provider indicates " +
                                "it cannot create DataAdapter instances: {0}", providerFactory);
                }
                else
                {
                    Console.Error.WriteLine("WARNING: Attempting to batch load data, however registered provider indicates " +
                                "it cannot create DataAdapter instances: {0}", providerFactory);
                }

                var table = new DataTable();
                foreach (var parameter in new List<DbParameter>(Parameters))
                {
                    var column = new DataColumn
                    {
                        ColumnName = parameter.SourceColumn,
                        DataType = ToType(parameter.DbType)
                    };
                    if (parameter.Size > 0)
                    {
                        column.MaxLength = parameter.Size;
                    }

                    table.Columns.Add(column);
                }

                ProcessRows(table);

                foreach (DataRow row in table.Rows)
                {
                    var command = CreateCommand(InsertStatement);
                    foreach (var parameter in Parameters)
                    {
                        var lparam = providerFactory.CreateParameter();
                        lparam.ParameterName = parameter.ParameterName;
                        lparam.Value = row[parameter.SourceColumn];
                        command.Parameters.Add(lparam);
                    }
                    command.ExecuteNonQuery();
                }
            }
        }


        DbCommand CreateCommand(string text)
        {
            var command = providerFactory.CreateCommand();
            command.CommandText = text;
            command.Connection = Connection;
            command.Transaction = Transaction;
            command.UpdatedRowSource = UpdateRowSource.None;
            command.CommandTimeout = CommandTimeout;
            return command;
        }

        static Type ToType(DbType dbType)
        {
            return dbType switch
            {
                DbType.DateTimeOffset => typeof(DateTimeOffset),
                DbType.DateTime or DbType.DateTime2 or DbType.Date or DbType.Time => typeof(DateTime),
                DbType.Currency or DbType.Decimal or DbType.VarNumeric => typeof(decimal),
                DbType.Double => typeof(double),
                DbType.Single => typeof(float),
                DbType.Guid => typeof(Guid),
                DbType.Int16 or DbType.Int32 => typeof(int),
                DbType.UInt16 or DbType.UInt32 => typeof(uint),
                DbType.Int64 => typeof(long),
                DbType.UInt64 => typeof(ulong),
                DbType.Boolean => typeof(bool),
                DbType.Byte => typeof(byte),
                DbType.SByte => typeof(sbyte),
                DbType.Binary => typeof(byte[]),
                DbType.Object => typeof(object),
                DbType.Xml => typeof(XmlNode),
                DbType.AnsiStringFixedLength or DbType.AnsiString or DbType.StringFixedLength or DbType.String or _ => typeof(string)

            };
        }
    }
}
