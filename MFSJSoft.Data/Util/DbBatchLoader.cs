using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MFSJSoft.Data.Util
{
    public class DbBatchLoader
    {

        public delegate void UpdateRow(DataRow row, object item);

        readonly DbProviderFactory providerFactory;

        public DbBatchLoader(DbProviderFactory providerFactory, DbConnection connection = null, DbTransaction transaction = null, bool noTimeout = false)
        {
            this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            Connection = connection;
            Transaction = transaction;
            NoTimeout = noTimeout;
        }

        public DbConnection Connection { get; set; }
        public DbTransaction Transaction { get; set; }
        public bool NoTimeout { get; set; }

        public string CreateStatement { get; set; }
        public string SelectStatement { get; set; }
        public string InsertStatement { get; set; }
        public IEnumerable<DbParameter> Parameters { get; set; }
        public System.Collections.IEnumerable InputData { get; set; }
        public UpdateRow RowDelegate { get; set; }
        

        public void Execute()
        {
            if (CreateStatement is not null)
            {
                using var command = CreateCommand(CreateStatement);
                command.ExecuteNonQuery();
            }

            if (providerFactory.CanCreateDataAdapter)
            {

                using var selectCommand = CreateCommand(SelectStatement ?? throw new ArgumentNullException(nameof(SelectStatement)));
                using var insertCommand = CreateCommand(InsertStatement ?? throw new ArgumentNullException(nameof(InsertStatement)));

                var adapter = providerFactory.CreateDataAdapter();
                adapter.SelectCommand = selectCommand;
                adapter.InsertCommand = insertCommand;
                adapter.UpdateBatchSize = 0;
                if (Parameters is not null)
                {
                    foreach (var parameter in Parameters)
                    {
                        adapter.InsertCommand.Parameters.Add(parameter);
                    }
                }

                var table = new DataTable();
                adapter.Fill(table);

                if (RowDelegate is null)
                {
                    throw new ArgumentNullException(nameof(RowDelegate));
                }

                foreach (var item in InputData ?? throw new ArgumentNullException(nameof(InputData)))
                {
                    var row = table.NewRow();
                    RowDelegate(row, item);
                    table.Rows.Add(row);
                }

                adapter.Update(table);
            }
            else
            {
                // TODO Warn provider cannot create data adapter; fall back to sequential inserts.
                // TODO Implement
                throw new NotImplementedException($"Only implemented for DataAdapters at this time; given provider factory indicates it cannot create adapters: {providerFactory}");
            }
        }


        DbCommand CreateCommand(string text)
        {
            var command = providerFactory.CreateCommand();
            command.CommandText = text;
            command.Connection = Connection;
            command.Transaction = Transaction;
            command.UpdatedRowSource = UpdateRowSource.None;
            if (NoTimeout)
            {
                command.CommandTimeout = 0;
            }
            return command;
        }
    }
}
