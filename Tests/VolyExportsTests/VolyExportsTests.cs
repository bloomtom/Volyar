using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using VolyExports;

namespace VolyExportsTests
{
    [TestClass]
    public class VolyExportsTests
    {
        [TestMethod]
        public void TestSerialization()
        {
            var deletion = new List<Deletion>()
            {
                new Deletion(TransactionTableType.MediaItem, 2)
            };
            var addition = new List<MediaItem>()
            {
                new MediaItem(){LibraryName = "abc", MediaId = 3, Name = "123"}
            };
            var modification = new List<MediaItem>()
            {
                 new MediaItem(){LibraryName = "def", MediaId = 4, Name = "456"}
            };
            var diff = new Differential() { CurrentKey = 1, Deletions = deletion, Additions = addition, Modifications = modification };

            var a = JsonConvert.SerializeObject(deletion);
            var b = JsonConvert.DeserializeObject<IEnumerable<Deletion>>(a);

            var c = JsonConvert.SerializeObject(addition);
            var d = JsonConvert.DeserializeObject<IEnumerable<MediaItem>>(c);

            var r = JsonConvert.SerializeObject(diff);
            var s = JsonConvert.DeserializeObject<Differential>(r);

            Assert.AreEqual(1, s.CurrentKey);
            Assert.AreEqual(2, s.Deletions.First().Key);
            Assert.AreEqual(3, s.Additions.First().MediaId);
            Assert.AreEqual(4, s.Modifications.First().MediaId);
        }
    }
}
