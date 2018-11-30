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
using Volyar.Media.Conversion;
using Volyar.Media.Scanning;
using Volyar.Models;

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
                    default:
                        string litePath = Path.Combine(Environment.CurrentDirectory, "temp.sqlite");
                        if (File.Exists(litePath)) { File.Delete(litePath); }
                        o.UseSqlite($"Data Source={litePath}");
                        break;
                }
                dbOptions.Database = (DbContextOptions<VolyContext>)o.Options;
            });

            var s = services.Where(x => x.ServiceType == typeof(VolyContext));
            string tempPath = Settings.TempPath;
            if (!Directory.Exists(tempPath))
            {
                tempPath = Environment.CurrentDirectory;
            }

            DQP.IDistinctQueueProcessor<IConversionItem> converter = new MediaConverter(
                Settings.FFmpegPath,
                Settings.FFprobePath,
                Settings.Mp4BoxPath,
                tempPath,
                Settings.Parallelization,
                LoggerFactory.CreateLogger<MediaConverter>());
            services.AddSingleton(converter);

            services.AddSingleton(new MediaScanner(dbOptions, Settings, converter, LoggerFactory.CreateLogger<MediaScanner>()));

            services.AddSingleton(Settings);

            services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            VolyContext context)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
            }

            app.UseMvc();

            app.UseStaticFiles("/voly/external/static");

            VolySeed.Initialize(context);
        }
    }
}
