using System.Collections.Generic;
using System.Data.SqlClient;
using Transformalize.Model;
using Transformalize.Rhino.Etl.Core;
using Transformalize.Rhino.Etl.Core.Operations;

namespace Transformalize.Operations {

    public class EntityDatabaseLoad : AbstractOperation {
        private readonly Entity _entity;

        public EntityDatabaseLoad(Entity entity) {
            _entity = entity;
        }

        public override IEnumerable<Row> Execute(IEnumerable<Row> rows) {
            using (var cn = new SqlConnection(_entity.OutputConnection.ConnectionString)) {
                cn.Open();
                foreach (var group in rows.Partition(_entity.OutputConnection.OutputBatchSize)) {
                    var cmd = new SqlCommand(
                        _entity.IsMaster() ?
                            _entity.EntitySqlWriter.UpsertSql(group):
                            _entity.EntitySqlWriter.UpdateSql(group),
                        cn
                    ) {CommandTimeout = 0};
                    cmd.Parameters.Add(new SqlParameter("@TflId", _entity.TflId));
                    _entity.RecordsAffected += cmd.ExecuteNonQuery();
                    Info("{0} | Processed {1} rows in {2} ({3})", _entity.ProcessName, _entity.RecordsAffected, Name, _entity.Name );
                }
                yield break;
            }
        }

    }
}