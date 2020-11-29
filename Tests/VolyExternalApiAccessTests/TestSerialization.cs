using Microsoft.VisualStudio.TestTools.UnitTesting;
using VolyExternalApiAccess.Darr;

namespace VolyExternalApiAccessTests
{
    [TestClass]
    public class TestSerialization
    {
        [TestMethod]
        public void SonarrV2()
        {
            var a = SonarrParsed.FromJson(Properties.Resources.SonarrV2ApiResult);
            Assert.AreEqual(1, a.Episodes.Count);
            Assert.AreEqual("My Show", a.ParsedEpisodeInfo.SeriesTitle);
        }

        [TestMethod]
        public void SonarrV3()
        {
            var a = SonarrParsed.FromJson(Properties.Resources.SonarrV3ApiResult);
            Assert.AreEqual(1, a.Episodes.Count);
            Assert.AreEqual("My Show", a.ParsedEpisodeInfo.SeriesTitle);
        }

        [TestMethod]
        public void RadarrV2()
        {
            var movies = Movie.FromJson(Properties.Resources.RadarrV2ApiResult);
            Assert.AreEqual(2, movies.Count);
            Assert.AreEqual("mymovie", movies[0].CleanTitle);
            Assert.AreEqual("tt2094766", movies[1].ImdbId);
        }
    }
}
