#region License
// /*
// See license included in this library folder.
// */
#endregion

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Transformalize.Libs.Rhino.Etl.Infrastructure;
using Transformalize.Main.Providers;

namespace Transformalize.Libs.Rhino.Etl.Operations {
    /// <summary>
    ///     Perform a batch command against SQL server
    /// </summary>
    public abstract class SqlBatchOperation : AbstractDatabaseOperation {
        private int _batchSize = 100;
        private int _timeout;

        protected SqlBatchOperation(AbstractConnection connection)
            : base(connection) {
            base.ParamPrefix = "@";
        }

        /// <summary>
        ///     Gets or sets the size of the batch.
        /// </summary>
        /// <value>The size of the batch.</value>
        public int BatchSize {
            get { return _batchSize; }
            set { _batchSize = value; }
        }

        /// <summary>
        ///     The timeout of the command set
        /// </summary>
        public int Timeout {
            get { return _timeout; }
            set { _timeout = value; }
        }

        /// <summary>
        ///     Executes this operation
        /// </summary>
        /// <param name="rows">The rows.</param>
        /// <returns></returns>
        public override IEnumerable<Row> Execute(IEnumerable<Row> rows) {
            Guard.Against<ArgumentException>(rows == null, "SqlBatchOperation cannot accept a null enumerator");
            using (var cn = (SqlConnection)Use.Connection(Connection))
            using (var transaction = BeginTransaction(cn)) {
                SqlCommandSet commandSet = null;
                CreateCommandSet(cn, transaction, ref commandSet, _timeout);
                foreach (var row in rows) {
                    var command = new SqlCommand();
                    PrepareCommand(row, command);
                    if (command.Parameters.Count == 0) //workaround around a framework bug
                    {
                        var guid = Guid.NewGuid();
                        command.Parameters.AddWithValue(guid.ToString(), guid);
                    }
                    commandSet.Append(command);
                    if (commandSet.CountOfCommands >= _batchSize) {
                        Trace("Executing batch of {0} commands", commandSet.CountOfCommands);
                        commandSet.ExecuteNonQuery();
                        CreateCommandSet(cn, transaction, ref commandSet, _timeout);
                    }
                }
                Trace("Executing final batch of {0} commands", commandSet.CountOfCommands);
                commandSet.ExecuteNonQuery();

                if (transaction != null) {
                    if (PipelineExecuter.HasErrors) {
                        Warn(null, "Rolling back transaction in {0}", Name);
                        transaction.Rollback();
                        Warn(null, "Rolled back transaction in {0}", Name);
                    } else {
                        Trace("Committing {0}", Name);
                        transaction.Commit();
                        Trace("Committed {0}", Name);
                    }
                }
            }
            yield break;
        }

        /// <summary>
        ///     Prepares the command from the given row
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="command">The command.</param>
        protected abstract void PrepareCommand(Row row, SqlCommand command);

        private static void CreateCommandSet(SqlConnection connection, SqlTransaction transaction, ref SqlCommandSet commandSet, int timeout) {
            if (commandSet != null)
                commandSet.Dispose();
            commandSet = new SqlCommandSet {
                Connection = connection,
                Transaction = transaction,
                CommandTimeout = timeout
            };
        }
    }
}