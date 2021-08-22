
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

    public class LoadTableProcessorConfiguration
    {
        public string CreateTempPrefix { get; set; }
        public IDictionary<DbType, string> DbTypeMapping { get; set; }
    }

    public class LoadTableProcessor : IDirectiveProcessor
    {

        public const string DirectiveName = "LoadTable";

        static readonly Regex ParamSplitPattern = new(@"\s*,\s*");

        public delegate bool WithLoader(string tableName, DbBatchLoader loader);

        
        readonly WithLoader callback;
        string createTempPrefix;
        IDictionary<DbType, string> dbTypeMapping;


        public LoadTableProcessor(WithLoader callback, string createTempPrefix = "CREATE TABLE", IDictionary<DbType, string> dbTypeMapping = null)
        {
            this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
            this.createTempPrefix = createTempPrefix;
            this.dbTypeMapping = dbTypeMapping;
        }

        public void InitProcessor(object configuration)
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

            return new DirectiveInitialization(new LoadTableInitializedState(tableName, createStatement, selectStatement, insertStatement, new List<DbParameter>(from pData in parameters select pData.ToParameter(context.ProviderFactory))));
        }

        public DirectiveInitialization SetupDirective(CompositeProcessorContext context, ScriptDirective directive, object initState)
        {
            throw new NotImplementedException();
        }

        public bool TryExecute(CompositeProcessorContext context, string text, ScriptDirective directive, object initState)
        {
            if (directive.Name != DirectiveName)
            {
                return false;
            }

            var loaderData = (LoadTableInitializedState) initState;
            var handled = callback(loaderData.TableName, new DbBatchLoader(context.ProviderFactory, context.Connection, context.Transaction)
            {
                CreateStatement = loaderData.CreateStatement,
                SelectStatement = loaderData.SelectStatement,
                InsertStatement = loaderData.InsertStatement,
                Parameters = loaderData.Parameters,
                NoTimeout = context.NoTimeout
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

    public class UnrecognizedTableException : Exception
    {

        internal UnrecognizedTableException(string tableName, ScriptDirective directive) : base($"Unrecognized table: {tableName}; Directive: {directive}")
        {
            TableName = tableName;
            Directive = directive;
        }

        public string TableName { get; }

        public ScriptDirective Directive { get; }
    }
}
