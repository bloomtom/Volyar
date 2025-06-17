using System.IO;
using System;
using Volyar.Models;
using Microsoft.Extensions.Logging;

namespace Volyar
{
    public static class ConfigTests
    {
        public static void Test(VSettings settings, ILogger log)
        {
            log.LogInformation("Running config tests...");
            foreach (var library in settings.Libraries)
            {
                if (library.SourceHandling.Equals("delete", StringComparison.InvariantCultureIgnoreCase) && library.DeleteWithSource)
                {
                    log.LogWarning("Library {Name} is set to SourceHandling:delete and DeleteWithSource:true. This is allowed, but is atypical.", library.Name);
                }

                TestStorageBackend(log, library);

                TestRemoteApi(log, library);
            }
            log.LogInformation("Config testing complete.");
        }

        private static void TestRemoteApi(ILogger log, Library library)
        {
            if (library.ApiIntegration == null) { return; }

            // Test API integration
            try
            {
                var fetcher = new VolyExternalApiAccess.ApiFetch(
                    library.ApiIntegration.Type,
                    library.ApiIntegration.Url,
                    library.ApiIntegration.ApiKey,
                    library.ApiIntegration.Username,
                    library.ApiIntegration.Password);
                fetcher.RetrieveVersionAsync().Wait();
            }
            catch (Exception ex)
            {
                log.LogError("Testing API integration for library '{Name}' failed. Exception: {ex}", library.Name, ex);
            }
        }

        private static void TestStorageBackend(ILogger log, Library library)
        {
            if (library.StorageBackend.DisableChecks) { return; }

            // Test storage backend
            try
            {
                const string testFilename = "VolyarStorageTestFileca10f8eb";
                var fileBackend = library.StorageBackend.RetrieveBackend();

                try
                {
                    fileBackend.UploadAsync(testFilename, new MemoryStream(), true).Wait();
                }
                catch (Exception ex)
                {
                    log.LogError("Testing storage backend for library '{library}' failed. Could not upload file {testFilename}. Exception: {ex}", library.Name, testFilename, ex);
                }
                try
                {
                    fileBackend.DeleteAsync(testFilename).Wait();
                }
                catch (Exception ex)
                {
                    log.LogError("Testing storage backend for library '{library}' failed. Could not upload file {testFilename}. Exception: {ex}", library.Name, testFilename, ex);
                }
            }
            catch (Exception ex)
            {
                log.LogError("Testing storage backend for library '{library}' failed. Exception: {ex}", library.Name, ex);
            }
        }
    }
}
