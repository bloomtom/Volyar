using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Volyar
{
    public class Program
    {
        public static readonly string settingsPath = Path.Join(Environment.CurrentDirectory, "vsettings.json");

        public static void Main(string[] args)
        {
            if (!File.Exists(settingsPath))
            {
                Console.WriteLine("Creating settings at: " + settingsPath);
                File.WriteAllText(settingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(new Models.VSettings(), Newtonsoft.Json.Formatting.Indented));
            }

            var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.VSettings>(File.ReadAllText(settingsPath),
                new Newtonsoft.Json.JsonSerializerSettings(){ ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace });
            
            CreateWebHostBuilder(args, $"http://{settings.Listen}:{settings.Port}")
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, string url) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>().UseUrls(new string[] { url });
    }
}
