using Runtime.Entity.RowItem;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Entity.Cache
{
    internal class RowInMemoryCache
    {
        #region Constructors
        private RowInMemoryCache(
            IImmutableDictionary<string, ProcedureRunCache> procedureRunMap)
        {
            ProcedureRunMap = procedureRunMap;
        }

        public RowInMemoryCache()
            : this(ImmutableDictionary<string, ProcedureRunCache>.Empty)
        {
        }

        public RowInMemoryCache(IEnumerable<RowBase> items)
            : this()
        {
            foreach (var item in items)
            {
                ProcedureRunMap = AppendItemToProcedureRunCache(item);
            }
        }
        #endregion

        public IImmutableDictionary<string, ProcedureRunCache> ProcedureRunMap { get; }

        public IEnumerable<RowBase> GetItems()
        {
            foreach (var procedureRun in ProcedureRunMap.Values)
            {
                yield return procedureRun.Row;
            }
        }

        public RowInMemoryCache AppendItem(RowBase item)
        {
            return new RowInMemoryCache(AppendItemToProcedureRunCache(item));
        }

        private IImmutableDictionary<string, ProcedureRunCache> AppendItemToProcedureRunCache(
            RowBase row)
        {
            switch (row)
            {
                case ProcedureRunRow r:
                    return AppendProcedureRun(r);
                case ProcedureRunTextRow rt:
                    return AppendProcedureRunText(rt);
                case ProcedureRunPlanRow rp:
                    return AppendProcedureRunPlan(rp);
                case ProcedureRunStepRow rs:
                    return AppendProcedureStepRun(rs);

                default:
                    throw new NotSupportedException(
                        $"Not supported row type:  {row.GetType().Name}");
            }
        }

        private IImmutableDictionary<string, ProcedureRunCache> AppendProcedureRun(
            ProcedureRunRow row)
        {
            var operationId = row.OperationId;

            if (ProcedureRunMap.ContainsKey(operationId))
            {
                var run = ProcedureRunMap[operationId];

                return ProcedureRunMap.SetItem(
                    operationId,
                    new ProcedureRunCache(row, run.Text, run.Plan));
            }
            else
            {
                return ProcedureRunMap.Add(operationId, new ProcedureRunCache(row));
            }
        }

        private IImmutableDictionary<string, ProcedureRunCache> AppendProcedureRunText(
            ProcedureRunTextRow row)
        {
            var operationId = row.OperationId;

            if (ProcedureRunMap.ContainsKey(operationId))
            {
                var run = ProcedureRunMap[operationId];

                return ProcedureRunMap.SetItem(
                    operationId,
                    new ProcedureRunCache(run.Row, row, run.Plan));
            }
            else
            {
                throw new NotSupportedException("Procedure run should come before text");
            }
        }

        private IImmutableDictionary<string, ProcedureRunCache> AppendProcedureRunPlan(
            ProcedureRunPlanRow row)
        {
            var operationId = row.OperationId;

            if (ProcedureRunMap.ContainsKey(operationId))
            {
                var run = ProcedureRunMap[operationId];

                return ProcedureRunMap.SetItem(
                    operationId,
                    new ProcedureRunCache(run.Row, run.Text, row));
            }
            else
            {
                throw new NotSupportedException("Procedure run should come before plan");
            }
        }

        private IImmutableDictionary<string, ProcedureRunCache> AppendProcedureStepRun(
            ProcedureRunStepRow row)
        {
            throw new NotImplementedException();
        }
    }
}