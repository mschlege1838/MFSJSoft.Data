
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MFSJSoft.Data.Scripting.Model;
using MFSJSoft.Data.Util;

namespace MFSJSoft.Data.Scripting.Processor
{

    /// <summary>
    /// Global configuration object for <see cref="LoadTableProcessor"/> directives. If corresponding properties
    /// are passed to the constructor of any <see cref="LoadTableProcessor"/>, they will override those provided
    /// in the global configuration.
    /// </summary>
    public class LoadTableProcessorConfiguration
    {

        /// <summary>
        /// (Optional) Create temporary table statemetn prefix.
        /// </summary>
        public string CreateTempPrefix { get; set; }

        /// <summary>
        /// (Optional) Mapping of <see cref="DbType"/> constants to their corresponding type name in the underlying DB.
        /// </summary>
        public IDictionary<DbType, string> DbTypeMapping { get; set; }
    }

    /// <summary>
    /// Directive processor for bulk table loads.
    /// </summary>
    public class LoadTableProcessor : IDirectiveProcessor
    {

        /// <summary>
        /// Bulk table load directive name
        /// </summary>
        public const string DirectiveName = "LoadTable";

        static readonly Regex ParamSplitPattern = new(@"\s*,\s*");

        /// <summary>
        /// Load table callback.
        /// </summary>
        /// <param name="tableName">Name of the table being loaded.</param>
        /// <param name="loader">Initialized <see cref="DbBatchLoader"/>.</param>
        /// <returns></returns>
        public delegate bool WithLoader(string tableName, DbBatchLoader loader);

        
        readonly WithLoader callback;
        string createTempPrefix;
        IDictionary<DbType, string> dbTypeMapping;

        /// <summary>
        /// <para>>Constructor.</para>
        /// <para>Default <see cref="DbType"/> to SQL type mapping is as follows (Non-ANSI types default to SQL Server types, but ANSI standard 
        /// is favored where permissible):</para>
        /// <code>
        /// DbType.DateTimeOffset => "datetimeoffset",
        /// DbType.DateTime or DbType.DateTime2 => "datetime",
        /// DbType.Date => "date",
        /// DbType.Time => "time",
        /// DbType.Currency => $"DECIMAL({size}, {(scale > 0 ? scale : 4)}",
        /// DbType.Decimal => $"DECIMAL({size}, {scale})",
        /// DbType.Double => "DOUBLE PRECISION",
        /// DbType.Single => "FLOAT",
        /// DbType.Guid => "CHAR(16)",
        /// DbType.Int16 or DbType.Int32 or DbType.UInt16 or DbType.UInt32 => "INTEGER",
        /// DbType.Int64 or DbType.UInt64 => "BIGINT",
        /// DbType.Boolean or DbType.Byte or DbType.SByte => "SMALLINT",
        /// DbType.Binary => $"BINARY VARYING({size})",
        /// DbType.AnsiStringFixedLength => $"CHARACTER({size})",
        /// DbType.AnsiString => $"CHARACTER VARYING({size})",
        /// DbType.Object => "BLOB",
        /// DbType.StringFixedLength => $"NATIONAL CHAR({size})",
        /// DbType.VarNumeric => $"NUMERIC({size}, {scale})",
        /// DbType.Xml => "XML",
        /// DbType.String or _ => $"NATIONAL CHAR VARYING({size})",
        /// </code>
        /// <para>If a <c>dbTypeMapping</c> is provided, but <see cref="DbType"/> is not contained in it, the resolved type
        /// will fall back to the above.</para>
        /// </summary>
        /// <param name="callback">Callback for client code to supply data to an initialized <see cref="DbBatchLoader"/></param>
        /// <param name="createTempPrefix">(Optional) Create temporary table prefix. Defaults to SQL Server's regular <c>CREATE TABLE</c>.
        /// (Temporary tables in SQL server use the regular <c>CREATE TABLE</c> statement, but are prefixed with a hash tag <c>#</c>)</param>
        /// <param name="dbTypeMapping">(Optional) Mapping from <see cref="DbType"/> constants to corresponding data type names in the underlying
        /// DB. Defaults to ANSI standard types, but falls back to SQL server types where no applicable ANSI type exists. Can be a format string;
        /// data type size is passed as <c>{0}</c>, and scale as <c>{1}</c>. Both size and scale default to zero if not applicable.</param>
        /// <exception cref="ArgumentNullException">If <c>callback</c> is <see langword="null" /></exception>
        public LoadTableProcessor(WithLoader callback, string createTempPrefix = "CREATE TABLE", IDictionary<DbType, string> dbTypeMapping = null)
        {
            this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
            this.createTempPrefix = createTempPrefix;
            this.dbTypeMapping = dbTypeMapping;
        }

        /// <summary>
        /// Initializes this processor.
        /// </summary>
        /// <param name="context">DB context.</param>
        /// <param name="configuration">Global configuration (if any).</param>
        public void InitProcessor(CompositeProcessorContext context, object configuration)
        {
            if (configuration is not null && configuration is LoadTableProcessorConfiguration lcfg)
            {
                if (createTempPrefix is null)
                {
                    createTempPrefix = lcfg.CreateTempPrefix ?? "CREATE TABLE";
                }
                if (dbTypeMapping is null && lcfg.DbTypeMapping is not null)
                {
                    dbTypeMapping = lcfg.DbTypeMapping;
                }
            }
        }

        /// <summary>
        /// Initializes this directive if applicable to current statement.
        /// </summary>
        /// <param name="context">DB context.</param>
        /// <param name="directive">Source information.</param>
        /// <returns></returns>
        /// <exception cref="InvalidDirectiveException"></exception>
        public DirectiveInitialization InitDirective(CompositeProcessorContext context, ScriptDirective directive)
        {
            if (directive.Name != DirectiveName)
            {
                return null;
            }

            if (directive.Arguments.Count < 2)
            {
                throw new InvalidDirectiveException("LoadTable directive must have at least 2 arguments.", directive);
            }

            var tableName = directive.Arguments[0];

            if (!bool.TryParse(directive.Arguments[1], out var createTemporary))
            {
                throw new InvalidDirectiveException($"LoadTable directive arguments[1] (2nd) must be {bool.TrueString} or {bool.FalseString}.", directive);
            }

            var parameters = new List<DbParameterData>();
            for (var i = 2; i < directive.Arguments.Count; ++i)
            {
                parameters.Add(GetParameter(directive.Arguments[i], i, directive, context.ProviderFactory));
            }

            var createStatement = createTemporary ? ToCreateStatement(tableName, parameters) : null;
            var selectStatement = ToSelectStatement(tableName, parameters);
            var insertStatement = ToInsertStatement(tableName, parameters);

            return new DirectiveInitialization(new LoadTableInitializedState(tableName, createStatement, selectStatement, insertStatement, from pData in parameters select pData.ToParameter(context.ProviderFactory)));
        }

        /// <summary>
        /// N/A
        /// </summary>
        /// <param name="context"></param>
        /// <param name="directive"></param>
        /// <param name="initState"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public DirectiveInitialization SetupDirective(CompositeProcessorContext context, ScriptDirective directive, object initState)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes the current statement if this directive is applied. Note this directive will always return
        /// <see langword="false"/>, as it is assumed to be annotated on subsequent statements in the source
        /// SQL script.
        /// </summary>
        /// <param name="context">DB context.</param>
        /// <param name="text">SQL sttatement text (ignored).</param>
        /// <param name="directive">Source information.</param>
        /// <param name="initState">Initialized state.</param>
        /// <returns><see langword="false" /></returns>
        /// <exception cref="UnrecognizedTableException">If the given <see cref="WithLoader"/> callback does not recognize
        /// the table name supplied with the directive. (I.e. returns <see langword="false"/></exception>
        public bool TryExecute(CompositeProcessorContext context, string text, ScriptDirective directive, object initState)
        {
            if (directive.Name != DirectiveName)
            {
                return false;
            }

            var loaderData = (LoadTableInitializedState) initState;
            var handled = callback(loaderData.TableName, new DbBatchLoader(context.ProviderFactory, context.Connection, 
                    context.Transaction, context.Logger)
            {
                CreateStatement = loaderData.CreateStatement,
                SelectStatement = loaderData.SelectStatement,
                InsertStatement = loaderData.InsertStatement,
                Parameters = loaderData.Parameters,
                CommandTimeout = context.CommandTimeout
            });

            if (!handled)
            {
                throw new UnrecognizedTableException(loaderData.TableName, directive);
            }

            return false;
        }

        DbParameterData GetParameter(string arg, int i, ScriptDirective directive, DbProviderFactory providerFactory)
        {
            var toks = ParamSplitPattern.Split(arg.Trim());
            if (toks.Length < 2)
            {
                throw new InvalidDirectiveException($"LoadTable arugments[{i}]: missing required arguments (sourceColumn, dbType): {arg}.", directive);
            }

            if (!Enum.TryParse<DbType>(toks[1], out var dbType))
            {
                throw new InvalidDirectiveException($"LoadTable arugments[{i}], 1: not a valid System.Data.DbType: {toks[1]}.", directive);
            }

            var size = 0;
            if (toks.Length > 2)
            {
                if (!int.TryParse(toks[2], out size))
                {
                    throw new InvalidDirectiveException($"LoadTable arugments[{i}], 2: not a valid int: {toks[2]}.", directive);
                }
            }

            byte scale = 0;
            if (toks.Length > 3)
            {
                if (!byte.TryParse(toks[3], out scale))
                {
                    throw new InvalidDirectiveException($"LoadTable arugments[{i}], 2: not a valid byte: {toks[3]}.", directive);
                }
            }


            return new DbParameterData(ToColumnDef(dbType, size, scale), $"@{toks[0]}", dbType, toks[0], size, scale);
        }


        static string ToInsertStatement(string tableName, IList<DbParameterData> parameters)
        {
            var result = new StringBuilder("INSERT INTO ").Append(tableName).Append(" (");

            var first = true;
            foreach (var pData in parameters)
            {
                if (first)
                    first = false;
                else
                    result.Append(',');

                result.Append(' ').Append(pData.SourceColumn);
            }

            result.Append(" ) VALUES (");

            first = true;
            foreach (var pData in parameters)
            {
                if (first)
                    first = false;
                else
                    result.Append(',');

                result.Append(' ').Append(pData.ParameterName);
            }

            result.Append(" )");

            return result.ToString();
        }

        static string ToSelectStatement(string tableName, IList<DbParameterData> parameters)
        {
            var result = new StringBuilder("SELECT");

            var first = true;
            foreach (var pData in parameters)
            {
                if (first)
                    first = false;
                else
                    result.Append(',');

                result.Append(' ').Append(pData.SourceColumn);
            }

            result.Append(" FROM ").Append(tableName);

            return result.ToString();
        }

        string ToCreateStatement(string tableName, IList<DbParameterData> parameters)
        {
            var result = new StringBuilder(createTempPrefix).Append('"').Append(tableName).Append("\" (");

            var first = true;
            foreach (var pData in parameters)
            {
                if (first)
                    first = false;
                else
                    result.Append(',');

                result.Append(' ').Append(pData.SourceColumn).Append(' ').Append(pData.ColumnDef);
            }

            result.Append(" )");

            return result.ToString();
        }

        string ToColumnDef(DbType type, int size = 0, byte scale = 0)
        {
            if (dbTypeMapping is not null && dbTypeMapping.ContainsKey(type))
            {
                return string.Format(dbTypeMapping[type], size, scale);
            }

            return type switch
            {
                DbType.DateTimeOffset => "datetimeoffset",
                DbType.DateTime or DbType.DateTime2 => "datetime",
                DbType.Date => "date",
                DbType.Time => "time",
                DbType.Currency => $"DECIMAL({size}, {(scale > 0 ? scale : 4)}",
                DbType.Decimal => $"DECIMAL({size}, {scale})",
                DbType.Double => "DOUBLE PRECISION",
                DbType.Single => "FLOAT",
                DbType.Guid => "CHAR(16)",
                DbType.Int16 or DbType.Int32 or DbType.UInt16 or DbType.UInt32 => "INTEGER",
                DbType.Int64 or DbType.UInt64 => "BIGINT",
                DbType.Boolean or DbType.Byte or DbType.SByte => "SMALLINT",
                DbType.Binary => $"BINARY VARYING({size})",
                DbType.AnsiStringFixedLength => $"CHARACTER({size})",
                DbType.AnsiString => $"CHARACTER VARYING({size})",
                DbType.Object => "BLOB",
                DbType.StringFixedLength => $"NATIONAL CHAR({size})",
                DbType.VarNumeric => $"NUMERIC({size}, {scale})",
                DbType.Xml => "XML",
                DbType.String or _ => $"NATIONAL CHAR VARYING({size})",
            };
        }


        class DbParameterData
        {
            internal DbParameterData(string columnDef, string parameterName, DbType dbType, string sourceColumn, int size, byte scale)
            {
                ColumnDef = columnDef;
                ParameterName = parameterName;
                DbType = dbType;
                SourceColumn = sourceColumn;
                Size = size;
                Scale = scale;
            }

            internal string ColumnDef { get; }
            internal string ParameterName { get; }
            internal DbType DbType { get; }
            internal string SourceColumn { get; }
            internal int Size { get; }
            internal byte Scale { get; }

            internal DbParameter ToParameter(DbProviderFactory providerFactory)
            {
                var parameter = providerFactory.CreateParameter();
                parameter.ParameterName = ParameterName;
                parameter.DbType = DbType;
                parameter.SourceColumn = SourceColumn;
                if (Scale > 0)
                {
                    parameter.Precision = (byte) Size;
                    parameter.Scale = Scale;
                }
                else
                {
                    parameter.Size = Size;
                }

                return parameter;
            }
        }

        
    }

    class LoadTableInitializedState
    {
        internal LoadTableInitializedState(string tableName, string createStatement, string selectStatement, string insertStatement, IEnumerable<DbParameter> parameters)
        {
            TableName = tableName;
            CreateStatement = createStatement;
            SelectStatement = selectStatement;
            InsertStatement = insertStatement;
            Parameters = parameters;
        }

        internal string TableName { get; }
        internal string CreateStatement { get; }
        internal string SelectStatement { get; }
        internal string InsertStatement { get; }
        internal IEnumerable<DbParameter> Parameters { get; }
    }

    /// <summary>
    /// Thrown when a <see cref="LoadTableProcessor.WithLoader"/> callback does not recognize the table name
    /// provided in the source SQL script. (I.e. returns <see langword="false" />
    /// </summary>
    public class UnrecognizedTableException : Exception
    {

        internal UnrecognizedTableException(string tableName, ScriptDirective directive) : base($"Unrecognized table: {tableName}; Directive: {directive}")
        {
            TableName = tableName;
            Directive = directive;
        }


        /// <summary>
        /// Table name in question.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Source information.
        /// </summary>
        public ScriptDirective Directive { get; }
    }
}
