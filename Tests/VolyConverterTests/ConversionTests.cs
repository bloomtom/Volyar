using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using VolyConverter.Conversion;
using VolyConverter.Scanning;
using VolyDatabase;
using Force.DeepCloner;
using Microsoft.Data.Sqlite;
using VolyConverter.Complete;
using VolyConverter;
using VolyConverter.Plugin;
using DEnc.Models;

namespace VolyConverterTests
{
    [TestClass]
    public class ConversionTests
    {
        [TestMethod]
        public void TestConversion()
        {
            var globalTemp = Path.Join(Environment.CurrentDirectory, "testing\\globaltemp");
            Directory.CreateDirectory(globalTemp);

            var inputDirectory = Path.Join(Environment.CurrentDirectory, "testing\\input");
            Directory.CreateDirectory(inputDirectory);

            if (!File.Exists("testing\\input\\test5.mkv")) { File.Copy(Path.Join(Environment.CurrentDirectory, "..\\..\\..\\test5.mkv"), "testing\\input\\test5.mkv"); }
            if (!File.Exists("testing\\input\\test52.mkv")) { File.Copy(Path.Join(Environment.CurrentDirectory, "..\\..\\..\\test52.mkv"), "testing\\input\\test52.mkv"); }
            if (!File.Exists("testing\\input\\testfile.ogg")) { File.Copy(Path.Join(Environment.CurrentDirectory, "..\\..\\..\\testfile.ogg"), "testing\\input\\testfile.ogg"); }
            if (!File.Exists("testing\\input\\testfile2.ogg")) { File.Copy(Path.Join(Environment.CurrentDirectory, "..\\..\\..\\testfile2.ogg"), "testing\\input\\testfile2.ogg"); }

            var libraryTemp = Path.Join(Environment.CurrentDirectory, "testing\\librarytemp");
            Directory.CreateDirectory(libraryTemp);

            var outputDirectory = Path.Join(Environment.CurrentDirectory, "testing\\output");
            Directory.CreateDirectory(outputDirectory);

            string litePath = Path.Combine(Environment.CurrentDirectory, "temp.sqlite");
            if (File.Exists(litePath)) { File.Delete(litePath); }

            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            try
            {
                var dbBuilder = new DbContextOptionsBuilder<VolyContext>()
                    .UseSqlite(connection);
                MediaDatabase db = new MediaDatabase
                {
                    Database = dbBuilder.Options
                };

                var logFactory = new LoggerFactory();

                DQP.IDistinctQueueProcessor<IConversionItem> converter = new MediaConversionQueue(
                    "ffmpeg",
                    "ffprobe",
                    "mp4box",
                    globalTemp,
                    1,
                    new CompleteItems<IExportableConversionItem>(),
                    new Logger<MediaConversionQueue>(logFactory));
                var scanQueue = new LibraryScanningQueue(db, converter, new List<IConversionPlugin>(), new Logger<LibraryScanningQueue>(logFactory));

                var quality1 = new Quality(640, 480, 300, DEnc.H264Preset.ultrafast);
                var quality2 = new Quality(640, 480, 400, DEnc.H264Preset.ultrafast);
                var testLibrary = new Library()
                {
                    Name = "Test",
                    OriginPath = inputDirectory,
                    Qualities = new List<Quality>() { quality1, quality2 },
                    ValidExtensions = new HashSet<string>() { ".mp4", ".mkv", ".ogg" },
                    TempPath = libraryTemp
                };

                IStorage storage = new MStorage.FilesystemStorage.FilesystemStorage(outputDirectory);

                using var context = new VolyContext(dbBuilder.Options);
                VolySeed.Initialize(context, logFactory.CreateLogger("VolySeed"));

                scanQueue.ScheduleLibraryScan(testLibrary, storage, context);

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                while (converter.ItemsQueued.Count == 0)
                {
                    if (sw.ElapsedMilliseconds > 10000)
                    {
                        Assert.Fail("Disk scan took too long.");
                    }
                    System.Threading.Thread.Sleep(10);
                }

                var itemsProcessed = new Dictionary<string, IConversionItem>();
                int lastProgressesSeen = 0;
                int progressMovedEvents = 0;
                while (converter.ItemsQueued.Count > 0)
                {
                    foreach (var item in converter.ItemsProcessing)
                    {
                        Assert.IsTrue(item.Value.Quality.Where(x => x.Bitrate == quality1.Bitrate).SingleOrDefault() != null, "Item does not have the correct quality1 configuration.");
                        Assert.IsTrue(item.Value.Quality.Where(x => x.Bitrate == quality2.Bitrate).SingleOrDefault() != null, "Item does not have the correct quality2 configuration.");
                        if (!itemsProcessed.ContainsKey(item.Key))
                        {
                            itemsProcessed.Add(item.Key, item.Value.DeepClone());
                            Assert.AreEqual(item.Value.SourcePath, itemsProcessed[item.Key].SourcePath);
                        }
                        else
                        {
                            int progressesSeen = 0;
                            var updateProgresses = item.Value.Progress.GetEnumerator();
                            var currentProgresses = itemsProcessed[item.Key].Progress.GetEnumerator();
                            while (updateProgresses.MoveNext())
                            {
                                progressesSeen++;
                                if (currentProgresses.MoveNext())
                                {
                                    Assert.AreEqual(currentProgresses.Current.Description, updateProgresses.Current.Description);
                                    Assert.IsTrue(updateProgresses.Current.Progress >= currentProgresses.Current.Progress, "Progress not expected to go backwards.");
                                    if (updateProgresses.Current.Progress > currentProgresses.Current.Progress) { progressMovedEvents++; }
                                }
                            }
                            lastProgressesSeen = progressesSeen;

                            item.Value.DeepCloneTo(itemsProcessed[item.Key]);
                        }
                    }
                    System.Threading.Thread.Sleep(10);
                }

                Assert.IsTrue(lastProgressesSeen > 1);
                Assert.IsTrue(progressMovedEvents > 1);

            }
            finally
            {
                connection.Close();
            }
        }
    }
}
