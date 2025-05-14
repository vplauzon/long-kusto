using Runtime.Entity.RowItem;

namespace Runtime.Entity.Cache
{
    internal class ProcedureRunCache : CacheBase<ProcedureRunRow>
    {
        public ProcedureRunCache(ProcedureRunRow row)
            : base(row)
        {
        }

        public ProcedureRunCache(
            ProcedureRunRow row,
            ProcedureRunTextRow? text,
            ProcedureRunPlanRow? plan)
            : base(row)
        {
            Text = text;
            Plan = plan;
        }

        public ProcedureRunTextRow? Text { get; }

        public ProcedureRunPlanRow? Plan { get; }
    }
}