using System.Collections.Immutable;
using System.Linq;

namespace FlowPlanning.Parsing
{
    internal record Script(StatementScript[] Statements)
    {
        /// <summary>
        /// Checking a few things:  only last element is a return
        /// </summary>
        public void StaticAnalysis()
        {
            CheckReturns(Statements, null);
            CheckPrefix(Statements);
        }

        public Script TransformToReferenceReturnOnly()
        {
            return new Script(TransformToReferenceReturnOnly(Statements));
        }

        private void CheckReturns(
            IEnumerable<StatementScript> statements,
            bool? sequenceWithReturn)
        {   //  First check for this sequence
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
            //  Then recursively check for sub sequences
            foreach (var statement in statements)
            {
                if (statement.InnerStatement.ForEach != null)
                {
                    throw new NotImplementedException();
                }
                else if (statement.InnerStatement.Union != null)
                {
                    throw new NotImplementedException();
                }
            }
        }

        private void CheckPrefix(StatementScript[] statements)
        {   //  First check prefix for each statement
            var both = statements
                .FirstOrDefault(s => s.Prefix.ReturnPrefix && s.Prefix.LetIdPrefix != null);

            if (both != null)
            {
                throw new PlanningException("Can't have both let and return on a statement");
            }
            //  Then check recursively for sub sequences
            foreach (var statement in statements)
            {
                if (statement.InnerStatement.ForEach != null)
                {
                    throw new NotImplementedException();
                }
                else if (statement.InnerStatement.Union != null)
                {
                    throw new NotImplementedException();
                }
            }
        }

        private StatementScript[] TransformToReferenceReturnOnly(StatementScript[] statements)
        {
            var last = statements.LastOrDefault();

            if (last != null
                && last.Prefix.ReturnPrefix
                && last.InnerStatement.ReferencedIdentifier == null)
            {
                const string RETURN_IDENTIFIER = "$returnValue";
                var newRealStatement = new StatementScript(
                    new PrefixScript(RETURN_IDENTIFIER, false),
                    last.InnerStatement);
                var newReferenceStatement = new StatementScript(
                    new PrefixScript(null, true),
                    new InnerStatementScript(
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        RETURN_IDENTIFIER));

                return statements
                    .SkipLast(1)
                    .Append(newRealStatement)
                    .Append(newReferenceStatement)
                    .ToArray();
            }

            return statements;
        }
    }
}