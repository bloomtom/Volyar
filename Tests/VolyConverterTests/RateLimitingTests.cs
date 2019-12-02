using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using VolyConverter;

namespace VolyConverterTests
{
    [TestClass]
    public class RateLimitingTests
    {

        [TestMethod]
        public void TestRateLimiting()
        {
            TimeSpan rateTime = TimeSpan.FromMilliseconds(200);
            var limiter = new RateLimiter(rateTime, null, TimeSpan.FromMilliseconds(50));
            int i = 0;
            Action increment = new Action(() => { System.Threading.Interlocked.Increment(ref i); });

            var sw = new System.Diagnostics.Stopwatch();

            sw.Start();
            limiter.AddItem(new RateLimitedItem("A", increment));
            limiter.AddItem(new RateLimitedItem("A", increment));
            System.Threading.Thread.Sleep(50);
            limiter.AddItem(new RateLimitedItem("A", increment));
            System.Threading.Thread.Sleep(50);
            limiter.AddItem(new RateLimitedItem("A", increment));
            limiter.WaitForCompletion();
            Assert.AreEqual(2, i);
            limiter.AddItem(new RateLimitedItem("A", increment));
            limiter.WaitForCompletion();
            Assert.AreEqual(3, i);

            sw.Stop();
            Assert.IsTrue(sw.ElapsedMilliseconds > rateTime.TotalMilliseconds * 2);
            Assert.IsTrue(sw.ElapsedMilliseconds < rateTime.TotalMilliseconds * 3);
        }
    }
}
