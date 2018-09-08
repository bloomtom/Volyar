using DEnc;
using DQP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Volyar.Media.Conversion
{

    /// <summary>
    /// A threaded queue based encapsulation around the DEnc encoder.
    /// </summary>
    public class MediaConverter : DistinctQueueProcessor<IConversionItem>
    {
        private readonly Encoder encoder;

        private readonly ILogger<MediaConverter> log;

        /// <param name="ffmpegPath">The path to an FFmpeg executable.</param>
        /// <param name="ffProbePath">The path to an FFprobe executable.</param>
        /// <param name="mp4boxPath">The path to an mp4box executable.</param>
        /// <param name="tempPath">A temp path to use for intermediary files during processing.</param>
        /// <param name="parallelization">The number of files to convert at once.</param>
        /// <param name="logger">A log receiver to capture ffmpeg/mp4box output.</param>
        public MediaConverter(string ffmpegPath, string ffProbePath, string mp4boxPath, string tempPath, int parallelization, ILogger<MediaConverter> logger)
        {
            log = logger;
            Parallelization = parallelization <= 0 ? 1 : parallelization;
            encoder = new Encoder(ffmpegPath, ffProbePath, mp4boxPath,
                new Action<string>(s => { log.LogInformation(s); }),
                new Action<string>(s => { log.LogDebug(s); }), tempPath);
        }

        protected override void Error(IConversionItem item, Exception ex)
        {
            log.LogError($"Exception while converting {item.ToString()} -- {ex.Message}");
            item.ErrorAction.Invoke(ex);
        }

        protected override void Process(IConversionItem item)
        {
            var dashResult = encoder.GenerateDash(item.SourcePath, item.OutputBaseFilename, item.Framerate, 0, item.Quality, item.DestinationDirectory, new Action<float>(x => { item.Progress = x; }));
            if (dashResult == null) { throw new Exception("Failed to convert item. Got null from generator Check the ffmpeg/mp4box log."); }
            item.CompletionAction.Invoke(dashResult);
        }
    }
}
