using Runtime.Entity.RowItem;
using System.Collections.Immutable;
using static System.Net.Mime.MediaTypeNames;
using System.Numerics;

namespace Runtime.Entity.Cache
{
    internal class ProcedureRunStepCache : CacheBase<ProcedureRunStepRow>
    {
        public ProcedureRunStepCache(ProcedureRunStepRow row)
            : base(row)
        {
            SubStepMap = ImmutableDictionary<long, ProcedureRunStepCache>.Empty;
        }

        public ProcedureRunStepCache(
            ProcedureRunStepRow row,
            IImmutableDictionary<long, ProcedureRunStepCache> subStepMap)
            : base(row)
        {
            SubStepMap = subStepMap;
        }

        public IImmutableDictionary<long, ProcedureRunStepCache> SubStepMap { get; }

        public ProcedureRunStepCache AppendStep(
            ProcedureRunStepRow row,
            IEnumerable<long> indexes)
        {
            if (SubStepMap.ContainsKey(indexes.First()))
            {
                var subStep = SubStepMap[indexes.First()];

                if (indexes.Count() == 1)
                {   //  The step belongs to the run
                    var newSubStepMap = SubStepMap.SetItem(
                        indexes.First(),
                        new ProcedureRunStepCache(row, subStep.SubStepMap));

                    return new ProcedureRunStepCache(Row, newSubStepMap);
                }
                else
                {   //  The step belongs to a sub step
                    var newSubStepMap = SubStepMap.SetItem(
                        indexes.First(),
                        subStep.AppendStep(row, indexes.Skip(1)));

                    return new ProcedureRunStepCache(Row, newSubStepMap);
                }
            }
            else if (indexes.Count() == 1)
            {   //  New step
                var newSubStepMap = SubStepMap.SetItem(
                    indexes.First(),
                    new ProcedureRunStepCache(row));

                return new ProcedureRunStepCache(Row, newSubStepMap);
            }
            else
            {
                throw new InvalidDataException("Root step should come before sub step");
            }
        }
    }
}