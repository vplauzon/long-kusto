using FlowPlanning.Parsing;
using System.Reflection;

namespace UnitTest
{
    public class BaseTest
    {
        protected virtual string GetResource(
            string resourceName,
            Assembly? resourceAssembly = null)
        {
            resourceAssembly = resourceAssembly ?? typeof(BaseTest).GetTypeInfo().Assembly;

            var fullResourceName = resourceAssembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(resourceName));

            if(fullResourceName == null)
            {
                throw new ArgumentException(
                    $"Can't find resource file '{resourceName}'",
                    nameof(fullResourceName));
            }

            using (var stream = resourceAssembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new ArgumentException(
                        $"Can't load resource file '{fullResourceName}'",
                        nameof(fullResourceName));
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