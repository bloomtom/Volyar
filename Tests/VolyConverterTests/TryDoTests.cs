using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VolyConverter.Conversion;

namespace VolyConverterTests
{
    [TestClass]
    public class TryDoTests
    {
        [TestMethod]
        public void TestTryDoSucceed()
        {
            TryDo.Try(new Action(() => { }), 1);
            TryDo.Try(new Action(() => { }), 3);
            TryDo.Try(new Action(() => { }), 3, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void TestTryDoIntermittent()
        {
            int i = 0;
            TryDo.Try(new Action(() =>
            {
                i++;
                if (i < 3) { throw new TimeoutException(); }
            }), 5);
        }

        [TestMethod]
        public void TestTryDoWait()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            int i = 0;
            TryDo.Try(new Action(() =>
            {
                i++;
                if (i < 4) { throw new TimeoutException(); }
            }), 5, TimeSpan.FromMilliseconds(100));
            sw.Stop();
            Assert.IsTrue(sw.ElapsedMilliseconds > 200 && sw.ElapsedMilliseconds < 600);
        }

        [TestMethod]
        public void TestTryDoExceed()
        {
            int i = 0;
            Assert.ThrowsException<TimeoutException>(new Action(() =>
            {
                TryDo.Try(new Action(() =>
                {
                    i++;
                    throw new TimeoutException();
                }), 5, TimeSpan.FromMilliseconds(10));
            }));
            Assert.AreEqual(5, i);
        }

        [TestMethod]
        public void TestTryDoInstantThrow()
        {
            int i = 0;
            Assert.ThrowsException<TaskCanceledException>(new Action(() =>
            {
                TryDo.Try(new Action(() =>
                {
                    i++;
                    throw new TaskCanceledException();
                }), 5, TimeSpan.FromMilliseconds(100));
            }));
            Assert.AreEqual(1, i);
        }
    }
}
