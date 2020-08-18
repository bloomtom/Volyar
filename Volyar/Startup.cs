using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using VolyConverter.Conversion;
using VolyConverter.Scanning;
using Volyar.Models;
using VolyDatabase;
using VolyConverter.Complete;
using NLog.Extensions.Logging;
using NLog.Web;
using VolyConverter;
using VolyConverter.Plugin;

namespace Volyar
{
    public class Startup
    {
        private ILoggerFactory LoggerFactory { get; }
        public IConfiguration Configuration { get; }
        public IHostEnvironment Env { get; }
        public VSettings Settings { get; }

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory, IHostEnvironment env)
        {
            Configuration = configuration;
            LoggerFactory = loggerFactory;
            Env = env;

            if (File.Exists(Program.SettingsPath))
            {
                Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<VSettings>(File.ReadAllText(Program.SettingsPath),
                    new Newtonsoft.Json.JsonSerializerSettings() { ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace });
            }
            else
            {
                Settings = new VSettings();
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var builder = services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            if (Env.IsDevelopment())
            {
                builder.AddRazorRuntimeCompilation();
            }

            services.AddOptions();
            services.Configure<VSettings>(Configuration.GetSection("VSettings"));
            MediaDatabase dbOptions = AddDatabase(services);

            ICompleteItems<IExportableConversionItem> completeQueue = new CompleteItems<IExportableConversionItem>(Settings.CompleteQueueLength);
            services.AddSingleton(completeQueue);

            MediaConversionQueue converter = new MediaConversionQueue(
                Settings.FFmpegPath,
                Settings.FFprobePath,
                Settings.Mp4BoxPath,
                GetTemp(),
                Settings.Parallelization,
                completeQueue,
                LoggerFactory.CreateLogger<MediaConversionQueue>());
            services.AddSingleton(converter);

            RateLimiter rateLimiter = new RateLimiter(TimeSpan.FromSeconds(10), LoggerFactory.CreateLogger<RateLimiter>());
            services.AddSingleton(rateLimiter);
            List<IConversionPlugin> plugins = GeneratePlugins(rateLimiter);

            services.AddSingleton(new LibraryScanningQueue(dbOptions, converter, plugins, LoggerFactory.CreateLogger<LibraryScanningQueue>()));

            services.AddSingleton(Settings);

            services.AddAuthorization();

            services.AddHttpClient();
        }

        private static List<IConversionPlugin> GeneratePlugins(RateLimiter rateLimiter)
        {
            return new List<IConversionPlugin>
            {
                new PostConversionPlugin("WebHook", (args) =>
                {
                    if (args.Library is Models.Library library)
                    {
                        if (library?.WebHooks == null) { return; }
                        foreach (var hook in library.WebHooks)
                        {
                            rateLimiter.AddItem(new RateLimitedItem($"WebHook {hook.Url}", () => { hook.CallAsync("").Wait(); }));
                        }
                    }
                }),
                new PreConversionPlugin("ApiIntegration", (args) =>
                {
                    if (args.Library is Models.Library library)
                    {
                        if (library?.ApiIntegration == null) { return; }
                        var g = library.ApiIntegration;
                        VolyExternalApiAccess.ApiValue metadata = null;
                        try
                        {
                            var fetcher = new VolyExternalApiAccess.ApiFetch(g.Type, g.Url, g.ApiKey, g.Username, g.Password);
                            metadata = fetcher.RetrieveInfo(args.ConversionItem?.SourcePath ?? args.MediaItem.SourcePath);
                            if (metadata != null)
                            {
                                args.MediaItem.SeriesName = metadata.SeriesTitle ?? args.MediaItem.SeriesName;
                                args.MediaItem.Name = metadata.Title ?? args.MediaItem.Name;
                                args.MediaItem.SeasonNumber = metadata.SeasonNumber;
                                args.MediaItem.EpisodeNumber = metadata.EpisodeNumber;
                                args.MediaItem.AbsoluteEpisodeNumber = metadata.AbsoluteEpisodeNumber;
                                args.MediaItem.ImdbId = string.IsNullOrWhiteSpace(metadata.ImdbId) ? null : metadata.ImdbId;
                                args.MediaItem.TmdbId = metadata.TmdbId?.ToString();
                                args.MediaItem.TvdbId = metadata.TvdbId?.ToString();
                                args.MediaItem.TvmazeId = metadata.TvMazeId?.ToString();

                                if (args.ConversionItem != null && args.ConversionItem is ConversionItem conversionItem)
                                {
                                    if (g.Type == "radarr")
                                    {
                                        conversionItem.Tune = Tune.Film;
                                    }
                                    foreach (var genre in metadata?.Genres)
                                    {
                                        switch (genre.ToLowerInvariant())
                                        {
                                            case "animation":
                                            case "anime":
                                                conversionItem.Tune = Tune.Animation;
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    conversionItem.Series = args.MediaItem.SeriesName;
                                    conversionItem.Title = args.MediaItem.Name;
                                }
                            }
                            else
                            {
                                if (g.CancelIfUnavailable && args.ConversionItem != null)
                                {
                                    args.ConversionItem.CancellationToken.Cancel();
                                }
                            }
                        }
                        finally
                        {
                            if(metadata == null && g.CancelIfUnavailable && args.ConversionItem != null)
                            {
                                args.ConversionItem.ErrorReason = "Cancelled: No API data found.";
                                args.Log.LogWarning($"ApiIntegration retrieve failed for {args.ConversionItem.SourcePath}. Cancelling item.");
                                args.ConversionItem.CancellationToken.Cancel();
                            }
                        }
                    }
                })
            };
        }

        private string GetTemp()
        {
            string tempPath = Settings.TempPath;
            if (!Directory.Exists(tempPath))
            {
                tempPath = Environment.CurrentDirectory;
            }

            return tempPath;
        }

        private MediaDatabase AddDatabase(IServiceCollection services)
        {
            MediaDatabase dbOptions = new MediaDatabase();

            string connectionString = Settings.DatabaseConnection;
            if (Settings.DatabaseType == "temp")
            {
                string litePath = Path.Combine(Environment.CurrentDirectory, "temp.sqlite");
                if (File.Exists(litePath)) { File.Delete(litePath); }
                connectionString = $"Data Source={litePath}";
            }

            services.AddSingleton<IDapperConnection>(new DapperConnection(Settings.DatabaseType, connectionString));
            services.AddDbContextPool<VolyContext>((o) =>
            {
                switch (Settings.DatabaseType)
                {
                    case "temp":
                    case "sqlite":
                        o.UseSqlite(connectionString);
                        break;
                    case "sqlserver":
                        o.UseSqlServer(connectionString);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"The database settings {Settings.DatabaseType} is not a valid option. Only sqlite, sqlserver, mysql, and temp are allowed.");
                }
                dbOptions.Database = (DbContextOptions<VolyContext>)o.Options;
            });
            return dbOptions;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IHostApplicationLifetime applicationLifetime,
            IHostEnvironment env,
            ILoggerFactory loggerFactory,
            LibraryScanningQueue libraryScanningQueue,
            MediaConversionQueue mediaConversionQueue,
            VolyContext context)
        {
            var log = loggerFactory.CreateLogger("Volyar.Startup");

            applicationLifetime.ApplicationStopping.Register(() =>
            {
                log.LogInformation("Application stopping...");
                libraryScanningQueue.Cancel();
                mediaConversionQueue.Cancel();
                if (mediaConversionQueue.ItemsProcessing.Count() > 0)
                {
                    log.LogInformation("Waiting for tasks to cancel...");
                    var waitTimer = new System.Diagnostics.Stopwatch();
                    waitTimer.Start();
                    while (mediaConversionQueue.ItemsQueued.Count() > 0 && libraryScanningQueue.ItemsQueued.Count() > 0 || waitTimer.ElapsedMilliseconds > 10000)
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                    if (mediaConversionQueue.ItemsQueued.Count() == 0)
                    {
                        log.LogInformation("All tasks stopped.");
                    }
                    else
                    {
                        log.LogWarning("Shutdown timeout expired and not all tasks were cancelled.");
                        Program.PrepareShutdown(ShutdownCodes.TasksNotCancelled);
                        return;
                    }
                }

                log.LogInformation("Application stopped.");
                Program.PrepareShutdown(ShutdownCodes.Normal);
            });

            if (!string.IsNullOrWhiteSpace(Settings.BasePath))
            {
                app.UsePathBase(new Microsoft.AspNetCore.Http.PathString(Settings.BasePath));
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseStaticFiles("/external/static");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            VolySeed.Initialize(context, log);
        }
    }
}
