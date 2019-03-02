using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VolyExternalApiAccess.Darr;
using VolyExternalApiAccess.Darr.Radarr;

namespace VolyExternalApiAccessTests
{
    internal class MockRadarr : RadarrQuery
    {
        private int recreated = 0;
        public int Recreated { get { return recreated; } }

        public MockRadarr(string baseUrl, string apiKey, TimeSpan cacheTimeout) : base(baseUrl, apiKey, null, null, cacheTimeout)
        {

        }

        protected override ICollection<Movie> Get()
        {
            System.Threading.Interlocked.Increment(ref recreated);
            return new List<Movie>()
            {
                new Movie()
                {
                    Added = DateTime.Now,
                    Title = "My Favorite Movie",
                    ImdbId = "1234567"
                },
                new Movie()
                {
                    Added = DateTime.Now,
                    Title = "My Favorite Movie 2",
                    ImdbId = "1234568"
                },
                new Movie()
                {
                    Added = DateTime.Now,
                    Title = "My Favorite Movie 3",
                    ImdbId = "1234569"
                }
            };
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
                new MockRadarr("http://mysite.com/radarr/", "abcd", TimeSpan.FromMilliseconds(1)),
                new MockRadarr("http://twosite.com/radarr", "abcd", TimeSpan.FromSeconds(5))
            };
            Assert.AreEqual("http://mysite.com/radarr", mocks[0].baseUrl);
            Assert.AreEqual("http://twosite.com/radarr", mocks[1].baseUrl);

            var reader = new Action(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    foreach (var mock in mocks)
                    {
                        int itemsCount = 0;
                        foreach (var item in mock.Where((x) => true))
                        {
                            itemsCount++;
                        }
                        Assert.AreEqual(3, itemsCount);

                        foreach (var item in mock.Where((x) => x.ImdbId == "1234568"))
                        {
                            Assert.AreEqual("1234568", item.ImdbId);
                        }
                        System.Threading.Thread.Yield();
                    }
                }
            });

            var readThread = new System.Threading.Thread(new System.Threading.ThreadStart(reader));
            readThread.Start();
            readThread.Join();

            Assert.IsTrue(mocks[0].Recreated > 1);
            Assert.IsTrue(mocks[0].Recreated < 200); // Should be 10-50
            Assert.AreEqual(1, mocks[1].Recreated);
        }
    }
}