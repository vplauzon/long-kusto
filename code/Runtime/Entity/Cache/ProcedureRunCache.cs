using Runtime.Entity.RowItem;

namespace Runtime.Entity.Cache
{
    internal class ProcedureRunCache : CacheBase<ProcedureRunRow>
    {
        public ProcedureRunCache(ProcedureRunRow row)
            : base(row)
        {
        }
    }
}