using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volyar;

namespace VolyTests
{
    [TestClass]
    public class TestMediaManager
    {
        [TestMethod]
        public void TestMediaManagerQuery()
        {
            var query = new MediaManagerQuery("library:asdf");
            Assert.AreEqual("asdf", query.LibraryName);

            query = new MediaManagerQuery("series:qwerty");
            Assert.AreEqual("qwerty", query.SeriesName);

            query = new MediaManagerQuery("episode:uiop");
            Assert.AreEqual("uiop", query.EpisodeName);

            query = new MediaManagerQuery("library:asdf series:qwerty episode:uiop");
            Assert.AreEqual("asdf", query.LibraryName);
            Assert.AreEqual("qwerty", query.SeriesName);
            Assert.AreEqual("uiop", query.EpisodeName);

            query = new MediaManagerQuery("library:as\\ df series:qwerty episode:uiop Hello!");
            Assert.AreEqual("as df", query.LibraryName);
            Assert.AreEqual("qwerty", query.SeriesName);
            Assert.AreEqual("uiop", query.EpisodeName);
            Assert.AreEqual("Hello!", query.GeneralQuery);

            query = new MediaManagerQuery("library:asdf Hello! series:qwerty");
            Assert.AreEqual("asdf", query.LibraryName);
            Assert.AreEqual("qwerty", query.SeriesName);
            Assert.AreEqual("Hello!", query.GeneralQuery);

            query = new MediaManagerQuery("library:asdf Hello! series:qwerty World");
            Assert.AreEqual("asdf", query.LibraryName);
            Assert.AreEqual("qwerty", query.SeriesName);
            Assert.AreEqual("Hello! World", query.GeneralQuery);

            query = new MediaManagerQuery("library:asdf library:qwerty");
            Assert.AreEqual("qwerty", query.LibraryName);
        }
    }
}
