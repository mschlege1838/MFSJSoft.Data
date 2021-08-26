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
    ///         <description>A select statement used to define table metadata.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="InsertStatement">InsertStatement</see></term>
    ///         <description>Parameterized, single-row insert statement with parameters matching the names of those in the
    ///         <see cref="Parameters">Parameters</see> property.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Parameters">Parameters</see></term>
    ///         <description>Repeatable <see cref="IEnumerable{T}"/> of <see cref="DbParameter"/> instances configured for batch
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
    /// </remarks>
    public class DbBatchLoader
    {

        public delegate void UpdateRow(DataRow row, object item);

        readonly DbProviderFactory providerFactory;

        public DbBatchLoader(DbProviderFactory providerFactory, DbConnection connection = null, DbTransaction transaction = null, ILogger logger = null)
        {
            this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            Connection = connection;
            Transaction = transaction;
            Logger = logger;
        }

        public DbConnection Connection { get; set; }
        public DbTransaction Transaction { get; set; }
        public ILogger Logger { get; set; }

        public int CommandTimeout { get; set; } = 30;
        public int UpdateBatchSize { get; set; } = 0;

        public string CreateStatement { get; set; }
        public string SelectStatement { get; set; }
        public string InsertStatement { get; set; }
        public IEnumerable<DbParameter> Parameters { get; set; }
        public System.Collections.IEnumerable InputData { get; set; }
        public UpdateRow RowDelegate { get; set; }
        

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
                adapter.Fill(table);

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
                foreach (var parameter in Parameters)
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
