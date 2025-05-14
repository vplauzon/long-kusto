using Azure.Identity;
using Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnitTest;

namespace IntegratedTest
{
    public class IntegratedTestBase : BaseTest
    {
        #region Inner Types
        private class MainSettings
        {
            public IDictionary<string, ProjectSetting>? Profiles { get; set; }

            public IDictionary<string, string> GetEnvironmentVariables()
            {
                if (Profiles == null)
                {
                    throw new InvalidOperationException(
                        "'profiles' element isn't present in 'launchSettings.json'");
                }
                if (Profiles.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No profile is configured within 'profiles' element isn't present "
                        + "in 'launchSettings.json'");
                }
                var profile = Profiles.First().Value;

                if (profile.EnvironmentVariables == null)
                {
                    throw new InvalidOperationException(
                        "'environmentVariables' element isn't present in 'launchSettings.json'");
                }

                return profile.EnvironmentVariables;
            }
        }

        private class ProjectSetting
        {
            public IDictionary<string, string>? EnvironmentVariables { get; set; }
        }
        #endregion

        #region Constructor
        static IntegratedTestBase()
        {
            ConfigFileToEnvironmentVariables();
            RuntimeGateway = CreateRuntimeGatewayAsync().Result;
            KustoDbUri = GetKustoDbUri();
        }

        private static void ConfigFileToEnvironmentVariables()
        {
            const string PATH = "Properties\\launchSettings.json";

            if (File.Exists(PATH))
            {
                var settingContent = File.ReadAllText(PATH);
                var mainSetting = JsonSerializer.Deserialize<MainSettings>(
                    settingContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                    ?? throw new InvalidOperationException("Can't read 'launchSettings.json'");
                var variables = mainSetting.GetEnvironmentVariables();

                foreach (var variable in variables)
                {
                    Environment.SetEnvironmentVariable(variable.Key, variable.Value);
                }
            }
        }

        private static async Task<RuntimeGateway> CreateRuntimeGatewayAsync()
        {
            var ct = new CancellationToken();
            var dataLakeRootUrl = Environment.GetEnvironmentVariable("dataLakeRootUrl");
            var credentials = new AzureCliCredential();
            var runtimeGateway = await RuntimeGateway.CreateAsync(
                credentials,
                new Version(),
                "LK-TEST",
                $"{dataLakeRootUrl!}/{DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss-fff")}",
                ct);

            return runtimeGateway;
        }

        private static Uri GetKustoDbUri()
        {
            var kustoDbUriText = Environment.GetEnvironmentVariable("kustoDbUri");
            var kustoDbUri = new Uri(kustoDbUriText!);

            return kustoDbUri;
        }
        #endregion

        protected static RuntimeGateway RuntimeGateway { get; }

        protected static Uri KustoDbUri { get; }

        protected override string GetResource(
            string resourceName,
            Assembly? resourceAssembly = null)
        {
            return base.GetResource(
                resourceName,
                resourceAssembly ?? this.GetType().Assembly);
        }
    }
}
