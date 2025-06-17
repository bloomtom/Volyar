using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace Volyar
{
    public enum ShutdownCodes
    {
        Normal = 0,
        VersionRequested = 1,
        Bootstrap = 2,
        SettingsParseError = 3,
        TasksNotCancelled = 4,
        GeneralError = 99
    }

    public class Program
    {
        public static string SettingsPath { get; private set; }
        public static readonly Version version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

        public static void Main(string[] args)
        {
            // NLog: setup the logger first to catch all errors
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                NLog.LogManager.LoadConfiguration("nlog.config");

                Options commandOptions = null;
                try
                {
                    Parser.Default.ParseArguments<Options>(args)
                        .WithParsed(o =>
                        {
                            commandOptions = o;
                        })
                        .WithNotParsed(e =>
                        {
                            HandleParseError(e, logger);
                        });
                    SettingsPath = commandOptions.SettingsPath;
                }
                catch (Exception ex)
                {
                    logger.Error("Failed to parse command line arguments " + ex.ToString());
                    Shutdown(ShutdownCodes.SettingsParseError);
                }

                logger.Info("Initialized Main");

                if (!File.Exists(SettingsPath))
                {
                    logger.Info("Creating settings at: " + SettingsPath);
                    File.WriteAllText(SettingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(new Models.VSettings(), Newtonsoft.Json.Formatting.Indented));
                }

                WriteoutSchema(SettingsPath);
                var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.VSettings>(File.ReadAllText(SettingsPath),
                    new Newtonsoft.Json.JsonSerializerSettings() { ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace });

                // Exit if this is a bootstrap run.
                if (commandOptions != null && commandOptions.Bootstrap)
                {
                    logger.Info("This was just a bootstrapping run. Exiting...");
                    Shutdown(ShutdownCodes.Bootstrap);
                }

                CreateWebHostBuilder(args, $"http://{settings.Listen}:{settings.Port}")
                    .Build()
                    .Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Top level exception caught. Terminating application.");
                Shutdown(ShutdownCodes.GeneralError);
            }
        }

        public static void PrepareShutdown(ShutdownCodes code)
        {
            NLog.LogManager.Shutdown();
            Environment.ExitCode = (int)code;
        }

        private static void Shutdown(ShutdownCodes code)
        {
            PrepareShutdown(code);
            Environment.Exit((int)code);
        }

        private static void WriteoutSchema(string path)
        {
            // Generate and write out scema for settings
            var generator = new Newtonsoft.Json.Schema.Generation.JSchemaGenerator();
            var schema = generator.Generate(typeof(Models.VSettings));
            using (StreamWriter file = File.CreateText(Path.Combine(Path.GetDirectoryName(path), "vsettings.schema.json")))
            using (var writer = new Newtonsoft.Json.JsonTextWriter(file))
            {
                schema.WriteTo(writer);
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

        private static void HandleParseError(IEnumerable<Error> errs, NLog.Logger log)
        {
            if (errs.Count() == 1)
            {
                var e = errs.First();
                if (e.Tag == ErrorType.VersionRequestedError || e.Tag == ErrorType.HelpRequestedError)
                {
                    Shutdown(ShutdownCodes.VersionRequested);
                }
            }

            log.Warn($"Failed to parse command: {string.Join('\n', errs)}");
            Shutdown(ShutdownCodes.SettingsParseError);
        }
    }
}
