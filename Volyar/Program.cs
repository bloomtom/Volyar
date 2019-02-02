using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace Volyar
{
    public class Program
    {
        public static readonly string settingsPath = Path.Join(Environment.CurrentDirectory, "vsettings.json");
        public static readonly Version version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

        public static void Main(string[] args)
        {
            // NLog: setup the logger first to catch all errors
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                NLog.LogManager.LoadConfiguration("nlog.config");
                logger.Info("Initialized Main");

                if (!File.Exists(settingsPath))
                {
                    logger.Info("Creating settings at: " + settingsPath);
                    File.WriteAllText(settingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(new Models.VSettings(), Newtonsoft.Json.Formatting.Indented));
                }

                var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.VSettings>(File.ReadAllText(settingsPath),
                    new Newtonsoft.Json.JsonSerializerSettings() { ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace });

                foreach (var library in settings.Libraries)
                {
                    if (library.SourceHandling.ToLowerInvariant() == "delete" && library.DeleteWithSource)
                    {
                        logger.Warn($"Library {library.Name} is set to SourceHandling:delete and DeleteWithSource:true. This is allowed, but is atypical.");
                    }
                }

                CreateWebHostBuilder(args, $"http://{settings.Listen}:{settings.Port}")
                    .Build()
                    .Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Top level exception caught. Terminating application.");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, string url) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
            })
            .UseNLog()
            .UseStartup<Startup>().UseUrls(new string[] { url });
    }
}
