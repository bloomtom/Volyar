using DEnc;
using DQP;
using Microsoft.Extensions.Logging;
using NaiveProgress;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VolyConverter.Complete;

namespace VolyConverter.Conversion
{
    /// <summary>
    /// A threaded queue based encapsulation around the DEnc encoder.
    /// </summary>
    public class MediaConversionQueue : DistinctQueueProcessor<IConversionItem>
    {
        private readonly Encoder encoder;

        private readonly ICompleteItems<IExportableConversionItem> completeItems;

        private readonly ILogger<MediaConversionQueue> log;

        private readonly int parallelization;

        /// <param name="ffmpegPath">The path to an FFmpeg executable.</param>
        /// <param name="ffProbePath">The path to an FFprobe executable.</param>
        /// <param name="mp4boxPath">The path to an mp4box executable.</param>
        /// <param name="tempPath">A temp path to use for intermediary files during processing.</param>
        /// <param name="parallelization">The number of files to convert at once.</param>
        /// <param name="logger">A log receiver to capture ffmpeg/mp4box output.</param>
        public MediaConversionQueue(string ffmpegPath, string ffProbePath, string mp4boxPath, string tempPath, int parallelization, ICompleteItems<IExportableConversionItem> completeItems, ILogger<MediaConversionQueue> logger)
        {
            this.completeItems = completeItems ?? throw new ArgumentNullException("completeItems", "completeItems cannot be null.");
            log = logger ?? throw new ArgumentNullException("logger", "logger cannot be null.");

            Parallelization = parallelization <= 0 ? 1 : parallelization;
            this.parallelization = Parallelization;
            encoder = new Encoder(ffmpegPath, ffProbePath, mp4boxPath,
                new Action<string>(s =>
                {
                    if (s != null)
                    {
                        log.LogInformation(s);
                    }
                }),
                new Action<string>(s =>
                {
                    if (s != null)
                    {
                        log.LogDebug(s);
                    }
                }),
                tempPath);
        }

        protected override void Error(IConversionItem item, Exception ex)
        {
            log.LogError($"Exception while converting {item.ToString()} -- {ex.Message}");
            item.ErrorAction.Invoke(ex);
            item.ErrorText = ex.Message;

            completeItems.Add(item);
        }

        protected override void Process(IConversionItem item)
        {
            if (item.CancellationToken.IsCancellationRequested)
            {
                HandleCancel(item);
                return;
            }

            var options = new H264EncodeOptions();
            switch (item.Tune)
            {
                case Tune.Film:
                    options.AdditionalVideoFlags.Add("-tune film");
                    break;
                case Tune.Grain:
                    options.AdditionalVideoFlags.Add("-tune grain");
                    break;
                case Tune.Animation:
                    options.AdditionalVideoFlags.Add("-tune animation");
                    break;
            }

            log.LogInformation($"Processing item {item.SourcePath}");

            try
            {
                var dashResult = encoder.GenerateDash(
                    inFile: item.SourcePath,
                    outFilename: item.OutputBaseFilename,
                    framerate: item.Framerate,
                    keyframeInterval: 0,
                    qualities: item.Quality,
                    options: options,
                    outDirectory: item.OutputPath,
                    progress: new NaiveProgress<IEnumerable<EncodeStageProgress>>(x => { item.Progress = x.Select(y => new DescribedProgress(y.Name, y.Progress)); }),
                    cancel: item.CancellationToken.Token);
                if (dashResult == null) { throw new Exception("Failed to convert item. Got null from generator. Check the ffmpeg/mp4box log."); }

                item.CompletionAction.Invoke(item, dashResult);

                completeItems.Add(item);
            }
            catch (OperationCanceledException)
            {
                log.LogInformation($"Task Cancelled: {item.SourcePath}");
                HandleCancel(item);
            }
        }

        private void HandleCancel(IConversionItem item)
        {
            if (item.ErrorText == null) { item.ErrorText = "Cancelled"; }
            completeItems.Add(item);
        }

        /// <summary>
        /// Pause the conversion queue.
        /// </summary>
        public void Pause()
        {
            Parallelization = 0;
        }

        /// <summary>
        /// Resume the conversion queue.
        /// </summary>
        public void Resume()
        {
            Parallelization = parallelization;
            for (int i = 0; i < parallelization; i++)
            {
                ManualStartWorker();
            }
        }

        /// <summary>
        /// Cancel all current conversion tasks.
        /// </summary>
        public void Cancel()
        {
            foreach (var item in ItemsQueued.Values)
            {
                item.CancellationToken.Cancel();
            }
        }

        /// <summary>
        /// Cancel a single conversion task by the name/path.
        /// </summary>
        /// <param name="sourcePath">The SourcePath property of the conversion item to cancel.</param>
        public bool Cancel(string sourcePath)
        {
            if (ItemsQueued.ContainsKey(sourcePath))
            {
                ItemsQueued[sourcePath].CancellationToken.Cancel();
                return true;
            }
            return false;
        }
    }
}
