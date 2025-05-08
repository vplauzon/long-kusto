using System.Collections.Immutable;
using System.Linq;

namespace FlowPlanning.Parsing
{
    internal record Script(StatementScript[] Statements)
    {
        const string RETURN_IDENTIFIER = "$returnValue";

        #region Static Analysis
        /// <summary>
        /// Checking a few things:  only last element is a return
        /// </summary>
        public void StaticAnalysis()
        {
            CheckReturns(Statements, null);
            CheckPrefix(Statements);
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
                    CheckReturns(statement.InnerStatement.Union.Statements, true);
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
                    CheckPrefix(statement.InnerStatement.Union.Statements);
                }
            }
        }
        #endregion

        #region TransformToReferenceReturnOnly
        public Script TransformToReferenceReturnOnly()
        {
            return new Script(TransformToReferenceReturnOnly(Statements));
        }

        private StatementScript[] TransformToReferenceReturnOnly(StatementScript[] statements)
        {
            var transformedStatements = statements
                .Select(s => TransformToReferenceReturnOnly(s))
                .ToArray();
            var last = transformedStatements.LastOrDefault();

            if (last != null
                && last.Prefix.ReturnPrefix
                && last.InnerStatement.ReferencedIdentifier == null)
            {
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
                        null,
                        RETURN_IDENTIFIER));

                return transformedStatements
                    .SkipLast(1)
                    .Append(newRealStatement)
                    .Append(newReferenceStatement)
                    .ToArray();
            }
            else
            {
                return transformedStatements;
            }
        }

        private StatementScript TransformToReferenceReturnOnly(StatementScript statement)
        {
            if (statement.InnerStatement.Query != null
                || statement.InnerStatement.ShowCommand != null
                || statement.InnerStatement.Command != null
                || statement.InnerStatement.Await != null
                || statement.InnerStatement.Append != null
                || statement.InnerStatement.ReferencedIdentifier != null)
            {
                return statement;
            }
            else if (statement.InnerStatement.ForEach != null)
            {
                throw new NotSupportedException();
                //return new StatementScript(
                //    statement.Prefix,
                //    new InnerStatementScript(
                //        null,
                //        null,
                //        null,
                //        new ForEachScript()));
            }
            else if (statement.InnerStatement.Union != null)
            {
                var unionScript = statement.InnerStatement.Union!;

                return new StatementScript(
                    statement.Prefix,
                    new InnerStatementScript(
                        null,
                        null,
                        null,
                        null,
                        new UnionScript(
                            unionScript.Iterator,
                            unionScript.ResultSet,
                            unionScript.Type,
                            unionScript.Properties,
                            TransformToReferenceReturnOnly(unionScript.Statements))));
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        #endregion
    }
}