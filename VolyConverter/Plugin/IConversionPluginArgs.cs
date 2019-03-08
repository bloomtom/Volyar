using Microsoft.Extensions.Logging;
using VolyConverter.Conversion;
using VolyConverter.Scanning;
using VolyDatabase;

namespace VolyConverter.Plugin
{
    public interface IConversionPluginArgs
    {
        IConversionItem ConversionItem { get; }
        ILibrary Library { get; }
        ILogger Log { get; }
        MediaItem MediaItem { get; }
        ConversionType Type { get; }
    }
}