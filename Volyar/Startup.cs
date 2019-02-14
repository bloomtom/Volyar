using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VolyConverter.Conversion;
using VolyConverter.Scanning;
using Volyar.Models;
using VolyDatabase;
using VolyConverter.Complete;
using NLog.Extensions.Logging;
using NLog.Web;
using VolyConverter;

namespace Volyar
{
    public class Startup
    {
        private ILoggerFactory LoggerFactory { get; }
        public IConfiguration Configuration { get; }
        public IHostingEnvironment Env { get; }
        public VSettings Settings { get; }

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory, IHostingEnvironment env)
        {
            Configuration = configuration;
            LoggerFactory = loggerFactory;
            Env = env;

            if (File.Exists(Program.settingsPath))
            {
                Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<VSettings>(File.ReadAllText(Program.settingsPath),
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

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

            var plugins = new List<ConversionPlugin>
            {
                (args) =>
                {
                    if (args.Library is Models.Library library)
                    {
                        if (library?.WebHooks == null) { return; }
                        foreach (var hook in library.WebHooks)
                        {
                            rateLimiter.AddItem(new RateLimitedItem($"WebHook {hook.Url}", () => { hook.CallAsync("").Wait(); }));
                        }
                    }
                }
            };

            services.AddSingleton(new LibraryScanningQueue(dbOptions, converter, plugins, LoggerFactory.CreateLogger<LibraryScanningQueue>()));

            services.AddSingleton(Settings);

            services.AddAuthorization();

            services.AddHttpClient();
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
            services.AddDbContextPool<VolyContext>((o) =>
            {
                switch (Settings.DatabaseType)
                {
                    case "sqlite":
                        o.UseSqlite(Settings.DatabaseConnection);
                        break;
                    case "sqlserver":
                        o.UseSqlServer(Settings.DatabaseConnection);
                        break;
                    case "mysql":
                        o.UseMySql(Settings.DatabaseConnection);
                        break;
                    case "temp":
                        string litePath = Path.Combine(Environment.CurrentDirectory, "temp.sqlite");
                        if (File.Exists(litePath)) { File.Delete(litePath); }
                        o.UseSqlite($"Data Source={litePath}");
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
            IApplicationLifetime applicationLifetime,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            LibraryScanningQueue libraryScanningQueue,
            MediaConversionQueue mediaConversionQueue,
            VolyContext context)
        {
            var log = loggerFactory.CreateLogger("Volyar.Startup");

            applicationLifetime.ApplicationStopping.Register(() =>
            {
                libraryScanningQueue.Cancel();
                mediaConversionQueue.Cancel();
                if (mediaConversionQueue.ItemsProcessing.Count() > 0)
                {
                    log.LogInformation("Waiting for tasks to cancel...");
                    var waitTimer = new System.Diagnostics.Stopwatch();
                    waitTimer.Start();
                    while (mediaConversionQueue.ItemsQueued.Count() > 0 || waitTimer.ElapsedMilliseconds > 10000)
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
                    }
                }
            });

            if (!string.IsNullOrWhiteSpace(Settings.BasePath))
            {
                app.UsePathBase(new Microsoft.AspNetCore.Http.PathString(Settings.BasePath));
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            app.UseStaticFiles("/external/static");

            VolySeed.Initialize(context, log);
        }
    }
}
