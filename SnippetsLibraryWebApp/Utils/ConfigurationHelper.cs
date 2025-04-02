using Microsoft.Extensions.Configuration;
using System.IO;

namespace SnippetsLibraryWebApp.Utils
{
    public static class ConfigurationHelper
    {
        public static IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            return builder.Build();
        }

        public static string GetConnectionString(string name = "DefaultConnection")
        {
            var configuration = GetConfiguration();
            return configuration.GetConnectionString(name);
        }
    }
}
