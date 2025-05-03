using GramParserLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QueryPlan.Parsing
{
    internal static class ScriptParser
    {
        private static readonly Grammar _grammar = CreateGrammar();

        #region Constructor
        private static Grammar CreateGrammar()
        {
            var grammarText = GetResource("Long-Kusto-Grammar.txt");
            var grammar = MetaGrammar.ParseGrammar(grammarText);

            if (grammar == null)
            {
                throw new InvalidOperationException("Couldn't create grammar");
            }

            return grammar;
        }

        private static string GetResource(string resourceName)
        {
            var type = typeof(ScriptParser);
            var assembly = type.GetTypeInfo().Assembly;
            var typeNamespace = type.Namespace;
            var fullResourceName = $"{typeNamespace}.{resourceName}";

            using (var stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new ArgumentException(
                        $"Can't find resource file '{resourceName}'",
                        nameof(resourceName));
                }
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();

                    return text;
                }
            }
        }
        #endregion

        public static Script ParseScript(string text)
        {
            var match = _grammar!.Match("main", text);

            if (match == null)
            {
                throw new InvalidOperationException($"Couldn't parse script '{text}'");
            }

            var output = match.ComputeTypedOutput<Script>();

            if (output == null)
            {
                throw new InvalidOperationException(
                    $"Couldn't compute output for script '{text}'");
            }

            return output;
        }
    }
}