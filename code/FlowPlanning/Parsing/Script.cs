using System.Collections.Immutable;
using System.Linq;

namespace FlowPlanning.Parsing
{
    internal record Script(StatementScript[] Statements)
    {
        /// <summary>
        /// Checking a few things:  only last element is a return
        /// </summary>
        internal void StaticAnalysis()
        {
            CheckReturns(Statements, null);
        }

        private void CheckReturns(
            IEnumerable<StatementScript> statements,
            bool? sequenceWithReturn)
        {
            var returnWhenShouldnt = statements
                .SkipLast(1)
                .FirstOrDefault(s => s.Prefix.ReturnPrefix);

            if (returnWhenShouldnt != null)
            {
                throw new PlanningException(
                    "Can only return on the last statement of a block");
            }
            if (sequenceWithReturn == true)
            {
                var last = statements.LastOrDefault();

                if (last == null)
                {
                    throw new PlanningException(
                        "There should be at least one statement in the block");
                }
                if (last.Prefix.ReturnPrefix == false)
                {
                    throw new PlanningException(
                        "Last statement of a block must return");
                }
            }
            else if (sequenceWithReturn == false)
            {
                var last = statements.LastOrDefault();

                if (last != null && last.Prefix.ReturnPrefix == true)
                {
                    throw new PlanningException(
                        "Can't return in this block");
                }
            }
        }
    }
}