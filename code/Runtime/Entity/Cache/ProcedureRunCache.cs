using Azure;
using Runtime.Entity.RowItem;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;

namespace Runtime.Entity.Cache
{
    internal class ProcedureRunCache : CacheBase<ProcedureRunRow>
    {
        public ProcedureRunCache(ProcedureRunRow row)
            : base(row)
        {
            StepMap = ImmutableDictionary<long, ProcedureRunStepCache>.Empty;
        }

        public ProcedureRunCache(
            ProcedureRunRow row,
            ProcedureRunTextRow? text,
            ProcedureRunPlanRow? plan,
            IImmutableDictionary<long, ProcedureRunStepCache> stepMap)
            : base(row)
        {
            Text = text;
            Plan = plan;
            StepMap = stepMap;
        }

        public ProcedureRunTextRow? Text { get; }

        public ProcedureRunPlanRow? Plan { get; }

        public IImmutableDictionary<long, ProcedureRunStepCache> StepMap { get; }

        public ProcedureRunStepCache NavigateToStep(IEnumerable<long> indexes)
        {
            var step = StepMap[indexes.First()];

            foreach (var i in indexes.Skip(1))
            {
                step = step.SubStepMap[i];
            }

            return step;
        }

        public ProcedureRunCache AppendStep(ProcedureRunStepRow row)
        {
            var indexes = row.GetStepIndexes();

            if (StepMap.ContainsKey(indexes[0]))
            {
                var step = StepMap[indexes[0]];

                if (indexes.Count() == 1)
                {   //  The step belongs to the run
                    var newStepMap = StepMap.SetItem(
                        indexes[0],
                        new ProcedureRunStepCache(row, step.SubStepMap));

                    return new ProcedureRunCache(Row, Text, Plan, newStepMap);
                }
                else
                {   //  The step belongs to a sub step
                    var newStepMap = StepMap.SetItem(
                        indexes[0],
                        step.AppendStep(row, indexes.Skip(1)));

                    return new ProcedureRunCache(Row, Text, Plan, newStepMap);
                }
            }
            else if (indexes.Count() == 1)
            {   //  New step
                var newStepMap = StepMap.SetItem(
                    indexes[0],
                    new ProcedureRunStepCache(row));

                return new ProcedureRunCache(Row, Text, Plan, newStepMap);
            }
            else
            {
                throw new InvalidDataException("Root step should come before sub step");
            }
        }
    }
}