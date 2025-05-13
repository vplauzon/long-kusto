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
            throw new NotImplementedException();
        }

        private IImmutableDictionary<string, ProcedureRunCache> AppendProcedureStepRun(
            ProcedureRunStepRow row)
        {
            throw new NotImplementedException();
        }
    }
}