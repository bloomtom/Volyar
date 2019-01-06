﻿using DEnc;
using DQP;
using Microsoft.Extensions.Logging;
using NaiveProgress;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VolyConverter.Conversion
{
    public interface IMediaConverter
    {
        void Process(IConversionItem item);
    }

    /// <summary>
    /// A threaded queue based encapsulation around the DEnc encoder.
    /// </summary>
    public class MediaConversionQueue : DistinctQueueProcessor<IConversionItem>
    {
        private readonly Encoder encoder;

        private readonly ILogger<MediaConversionQueue> log;

        /// <param name="ffmpegPath">The path to an FFmpeg executable.</param>
        /// <param name="ffProbePath">The path to an FFprobe executable.</param>
        /// <param name="mp4boxPath">The path to an mp4box executable.</param>
        /// <param name="tempPath">A temp path to use for intermediary files during processing.</param>
        /// <param name="parallelization">The number of files to convert at once.</param>
        /// <param name="logger">A log receiver to capture ffmpeg/mp4box output.</param>
        public MediaConversionQueue(string ffmpegPath, string ffProbePath, string mp4boxPath, string tempPath, int parallelization, ILogger<MediaConversionQueue> logger)
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
            item.ErrorText = ex.Message;
        }

        protected override void Process(IConversionItem item)
        {
            var options = new H264EncodeOptions();

            var dashResult = encoder.GenerateDash(
                inFile: item.SourcePath,
                outFilename: item.OutputBaseFilename,
                framerate: item.Framerate,
                keyframeInterval: 0,
                qualities: item.Quality,
                options: options,
                outDirectory: item.OutputPath,
                progress: new NaiveProgress<IEnumerable<EncodeStageProgress>>(x => { item.Progress = x.Select(y => new DescribedProgress(y.Name, y.Progress)); }));
            if (dashResult == null) { throw new Exception("Failed to convert item. Got null from generator Check the ffmpeg/mp4box log."); }
            item.CompletionAction.Invoke(item, dashResult);
        }
    }
}