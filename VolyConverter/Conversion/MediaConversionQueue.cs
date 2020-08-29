using DEnc;
using DEnc.Exceptions;
using DEnc.Models;
using DQP;
using Microsoft.Extensions.Logging;
using NaiveProgress;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VolyConverter.Complete;
using VolyExports;
using VolyFiles;

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
            this.completeItems = completeItems ?? throw new ArgumentNullException(nameof(completeItems), "completeItems cannot be null.");
            log = logger ?? throw new ArgumentNullException(nameof(logger), "logger cannot be null.");

            Parallelization = parallelization <= 0 ? 1 : parallelization;
            this.parallelization = Parallelization;
            encoder = new Encoder(ffmpegPath, ffProbePath, mp4boxPath, workingDirectory: tempPath);
        }

        protected override void Error(IConversionItem item, Exception ex)
        {
            log.LogError($"Exception while converting {item} -- {ex.Message}");
            item.ErrorAction.Invoke(ex);
            item.ErrorReason = ex.Message;
            item.ErrorDetail = ex.ToString();

            completeItems.Add(item);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Overzealous stylecop.")]
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
                var probeData = encoder.ProbeFile(item.SourcePath, out _);
                var dashConfig = new DashConfig(item.SourcePath, item.OutputPath, item.Quality, item.OutputBaseFilename)
                {
                    EnableStreamCopying = true,
                    Framerate = item.Framerate,
                    Options = options
                };
                var dashResult = encoder.GenerateDash(dashConfig, probeData,
                    progress: new NaiveProgress<double>((x) =>
                    {
                        item.Progress = new[] { new DescribedProgress("Conversion", x) };
                    }),
                    cancel: item.CancellationToken.Token);

                if (dashResult == null) { throw new Exception("Failed to convert item. Got null from generator. Check the ffmpeg/mp4box log."); }

                item.CompletionAction.Invoke(item, dashResult);

                completeItems.Add(item);
            }
            catch (FFMpegFailedException ex) when (ex is FFMpegFailedException || ex is Mp4boxFailedException || ex is DashManifestNotCreatedException)
            {
                if (ex.InnerException is OperationCanceledException)
                {
                    log.LogInformation($"Task Cancelled: {item.SourcePath}");
                    HandleCancel(item);
                    return;
                }

                var logItems = new List<string>()
                {
                    $"Failed to convert {item.SourcePath}",
                    $"ffmpeg command: {ex.FFmpegCommand?.RenderedCommand ?? "Unavailable"}"
                };
                string failureStage = "Unknown";
                switch (ex)
                {
                    case DashManifestNotCreatedException dex:
                        failureStage = "Manifest generation";
                        break;
                    case Mp4boxFailedException mpex:
                        failureStage = "DASHing/MP4Box";
                        logItems.Add($"MP4Box command: {mpex.MP4BoxCommand.RenderedCommand}");
                        break;
                    case FFMpegFailedException ffex:
                        failureStage = "Encoding/ffmpeg";
                        break;
                    default:
                        break;
                }

                logItems.Add($"Stack trace: {ex}");
                if (ex.Log != null && ex.Log.Length > 0)
                {
                    logItems.Add($"Process log: {ex.Log}");
                }

                string fullLog = string.Join('\n', logItems);
                log.LogWarning(fullLog);

                item.ErrorReason = $"Failed at step: {failureStage}. Message: {ex.Message}";
                item.ErrorDetail = fullLog;
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
            if (item.ErrorReason == null) { item.ErrorReason = "Cancelled"; }
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
