using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using VolyConverter.Complete;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace VolyConverterTests
{
    [TestClass]
    public class CompleteItemsTests
    {

        [TestMethod]
        public void TestCompleteItemsLimit()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var items = new CompleteItems<int>(0); });

            LimitTest(1);
            LimitTest(10);
            LimitTest(100);
            LimitTest(1000);
            LimitTest(5000);
        }

        [TestMethod]
        public void TestCompleteItemsConcurrent()
        {
            var items = new CompleteItems<int>(5000);

            var addAction = new Action(() =>
            {
                for (int i = 0; i < 2499; i++)
                {
                    items.Add(i);
                }
            });

            var t1 = new Task(addAction);
            var t2 = new Task(addAction);
            t1.Start();
            t2.Start();
            System.Threading.Thread.Sleep(1);
            Assert.AreEqual(0, items.First());
            t1.Wait();
            t2.Wait();
            Assert.AreEqual(2498, items.Last());

            Assert.AreEqual(2499 * 2, items.Count);
        }

        private void LimitTest(int limit)
        {
            ICompleteItems<int> items = new CompleteItems<int>(limit);

            Assert.AreEqual(0, items.Count);
            items.Add(7);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(7, items.First());

            for (int i = 2; i < limit; i++)
            {
                items.Add(i);
                Assert.AreEqual(i, items.Count);
            }
            if (limit > 1)
            {
                Assert.AreEqual(limit - 1, items.Count);
            }
            items.Add(0);
            Assert.AreEqual(limit, items.Count);
            items.Add(0);
            Assert.AreEqual(limit, items.Count);

            var sw = new Stopwatch();
            sw.Start();
            items.Add(0);
            sw.Stop();
            Assert.IsTrue(sw.Elapsed < TimeSpan.FromMilliseconds(1));

            sw.Restart();
            int a = items.First();
            sw.Stop();
            Assert.IsTrue(sw.Elapsed < TimeSpan.FromMilliseconds(1));
        }
    }
}
