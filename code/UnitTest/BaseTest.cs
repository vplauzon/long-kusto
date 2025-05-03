using FlowPlanning.Parsing;
using System.Reflection;

namespace UnitTest
{
    public class BaseTest
    {
        protected static string GetResource(string resourceName)
        {
            var type = typeof(BaseTest);
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
    }
}