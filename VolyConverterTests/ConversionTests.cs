﻿using Microsoft.EntityFrameworkCore;
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
                MediaDatabase db = new MediaDatabase();
                db.Database = dbBuilder.Options;

                var logFactory = new LoggerFactory();

                DQP.IDistinctQueueProcessor<IConversionItem> converter = new MediaConversionQueue(
                    "ffmpeg",
                    "ffprobe",
                    "mp4box",
                    globalTemp,
                    1,
                    new Logger<MediaConversionQueue>(logFactory));
                var scanQueue = new LibraryScanningQueue(db, false, false, converter, new Logger<LibraryScanningQueue>(logFactory));

                var quality1 = new DEnc.Quality(640, 480, 300, "ultrafast");
                var quality2 = new DEnc.Quality(640, 480, 400, "ultrafast");
                var testLibrary = new Library()
                {
                    Name = "Test",
                    OriginPath = inputDirectory,
                    Qualities = new List<DEnc.Quality>() { quality1, quality2 },
                    ValidExtensions = new HashSet<string>() { ".mp4", ".mkv", ".ogg" },
                    TempPath = libraryTemp
                };

                IStorage storage = new MStorage.FilesystemStorage.FilesystemStorage(outputDirectory);

                using (var context = new VolyContext(dbBuilder.Options))
                {
                    VolySeed.Initialize(context);

                    scanQueue.ScheduleLibraryScan(testLibrary, storage, context);

                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    while (converter.ItemsQueued.Count == 0)
                    {
                        if (sw.ElapsedMilliseconds > 1000000)
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

            }
            finally
            {
                connection.Close();
            }
        }
    }
}
