using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VolyExternalApiAccess;
using VolyExternalApiAccess.Darr;
using VolyExternalApiAccess.Darr.Radarr;

namespace VolyExternalApiAccessTests
{
    internal class MockRadarr(string baseUrl, string apiKey, DarrApiVersion apiVersion, TimeSpan cacheTimeout) : RadarrQuery(baseUrl, apiKey, apiVersion, username: null, password: null, cacheTimeout: cacheTimeout)
    {
        private int recreated = 0;
        public int Recreated { get { return recreated; } }

        protected override Task<ApiResponse<ICollection<Movie>>> GetMoviesAsync()
        {
            System.Threading.Interlocked.Increment(ref recreated);
            return Task.FromResult(new ApiResponse<ICollection<Movie>>(new List<Movie>()
            {
                new()
                {
                    Added = DateTime.Now,
                    Title = "My Favorite Movie",
                    ImdbId = "1234567"
                },
                new()
                {
                    Added = DateTime.Now,
                    Title = "My Favorite Movie 2",
                    ImdbId = "1234568"
                },
                new()
                {
                    Added = DateTime.Now,
                    Title = "My Favorite Movie 3",
                    ImdbId = "1234569"
                }
            }, System.Net.HttpStatusCode.OK));
        }
    }

    [TestClass]
    public class TestRadarrQuery
    {
        [TestMethod]
        public void ThreadedQuery()
        {
            var mocks = new List<MockRadarr>()
            {
                new("http://mysite.com/radarr/", "abcd", DarrApiVersion.V3, TimeSpan.FromMilliseconds(1)),
                new("http://twosite.com/radarr", "abcd", DarrApiVersion.V3, TimeSpan.FromSeconds(5))
            };
            Assert.AreEqual("http://mysite.com/radarr", mocks[0].baseUrl);
            Assert.AreEqual("http://twosite.com/radarr", mocks[1].baseUrl);

            var reader = new Task(async () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    foreach (var mock in mocks)
                    {
                        int itemsCount = 0;
                        foreach (var item in (await mock.WhereAsync((x) => true)).Value)
                        {
                            itemsCount++;
                        }
                        Assert.AreEqual(3, itemsCount);

                        foreach (var item in (await mock.WhereAsync((x) => x.ImdbId == "1234568")).Value)
                        {
                            Assert.AreEqual("1234568", item.ImdbId);
                        }
                        System.Threading.Thread.Yield();
                    }
                }
            });

            var readThread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                reader.RunSynchronously();
            }));
            readThread.Start();
            readThread.Join();

            Assert.IsTrue(mocks[0].Recreated > 1);
            Assert.IsTrue(mocks[0].Recreated < 200); // Should be 10-50
            Assert.AreEqual(1, mocks[1].Recreated);
        }
    }
}