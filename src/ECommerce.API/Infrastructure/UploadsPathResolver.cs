using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ECommerce.API.Infrastructure
{
    public static class UploadsPathResolver
    {
        public static string Resolve(IConfiguration configuration, IHostEnvironment environment)
        {
            var configuredPath = configuration["Storage:UploadsRootPath"];

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.Combine(environment.ContentRootPath, "uploads");
            }

            if (Path.IsPathRooted(configuredPath))
            {
                return Path.GetFullPath(configuredPath);
            }

            return Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
        }
    }
}
