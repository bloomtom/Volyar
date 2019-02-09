using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using VolyDatabase;

namespace VolyDatabaseTests
{
    [TestClass]
    public class DatabaseTests
    {
        private SqliteConnection GetDatabase()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return connection;
        }

        [TestMethod]
        public void TestMediaItem()
        {
            using (var dbConnection = GetDatabase())
            {
                var dbBuilder = new DbContextOptionsBuilder<VolyContext>().UseSqlite(dbConnection);

                using (var context = new VolyContext(dbBuilder.Options))
                {
                    VolySeed.Initialize(context, null);

                    var newMedia = new MediaItem()
                    {
                        SourcePath = "",
                        SourceModified = DateTime.Now,
                        SourceHash = "",
                        Duration = TimeSpan.FromSeconds(1),
                        IndexName = "",
                        IndexHash = "",
                        LibraryName = "Library",
                        Name = "",
                        SeriesName = ""
                    };
                    context.Media.Add(newMedia);
                    context.SaveChanges();

                    Assert.AreEqual(1, newMedia.MediaId);

                    context.TransactionLog.Add(new TransactionLog()
                    {
                        TableName = "MediaItem",
                        Type = TransactionType.Insert,
                        Key = newMedia.MediaId
                    });

                    context.SaveChanges();

                    Assert.AreEqual(1, context.TransactionLog.FirstOrDefaultAsync().Result.Key);
                }
            }
        }
    }
}
