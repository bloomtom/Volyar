using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Volyar
{
    public class Options
    {
        [Usage(ApplicationAlias = ">  dotnet volyar.dll")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Bootstrap a config file", new Options { SettingsPath = "vsettings.json", Bootstrap = true });
            }
        }

        [Option('s', "settings", HelpText = "A path to the settings file. If the file doesn't exist, it is created.")]
        public string SettingsPath { get; set; } = System.IO.Path.Join(Environment.CurrentDirectory, "vsettings.json");

        [Option("bootstrap", Default = false, HelpText = "Launch in bootstrap mode. Critical files will be created then the application will close.")]
        public bool Bootstrap { get; set; }
    }
}
