using Microsoft.VisualStudio.TestTools.UnitTesting;
using VolyExternalApiAccess.Darr;

namespace VolyExternalApiAccessTests
{
    [TestClass]
    public class TestSerialization
    {
        [TestMethod]
        public void Sonarr()
        {
            const string json = "{\"parsedEpisodeInfo\":{\"releaseTitle\":\"My.Show.S01E13.(Part.2)\",\"seriesTitle\":\"My Show\",\"seriesTitleInfo\":{\"title\":\"My Show\",\"titleWithoutYear\":\"My Show\",\"year\":0},\"quality\":{\"quality\":{\"id\":1,\"name\":\"SDTV\",\"source\":\"television\",\"resolution\":480},\"revision\":{\"version\":1,\"real\":0}},\"seasonNumber\":1,\"episodeNumbers\":[13],\"absoluteEpisodeNumbers\":[],\"specialAbsoluteEpisodeNumbers\":[],\"language\":\"english\",\"fullSeason\":false,\"isPartialSeason\":false,\"isSeasonExtra\":false,\"special\":false,\"releaseHash\":\"\",\"seasonPart\":0,\"isDaily\":false,\"isAbsoluteNumbering\":false,\"isPossibleSpecialEpisode\":false,\"isPossibleSceneSeasonSpecial\":false},\"series\":{\"title\":\"My Show\",\"sortTitle\":\"my show\",\"seasonCount\":1,\"status\":\"ended\",\"overview\":\"This is a story about my show.\",\"network\":\"My MX\",\"airTime\":\"00:00\",\"images\":[{\"coverType\":\"fanart\",\"url\":\"https://www.thetvdb.com/banners/fanart/original/abcd.jpg\"},{\"coverType\":\"banner\",\"url\":\"https://www.thetvdb.com/banners/graphical/abcd.jpg\"},{\"coverType\":\"poster\",\"url\":\"https://www.thetvdb.com/banners/posters/abcd.jpg\"}],\"seasons\":[{\"seasonNumber\":0,\"monitored\":false},{\"seasonNumber\":1,\"monitored\":true}],\"year\":2018,\"path\":\"/mnt/media/downloads/series/My Show\",\"profileId\":7,\"seasonFolder\":true,\"monitored\":true,\"useSceneNumbering\":true,\"runtime\":25,\"tvdbId\":123456,\"tvRageId\":0,\"tvMazeId\":12345,\"firstAired\":\"2018-01-01T00:00:00Z\",\"lastInfoSync\":\"2019-01-01T06:54:03.101071Z\",\"seriesType\":\"anime\",\"cleanTitle\":\"myshow\",\"imdbId\":\"tt1234567\",\"titleSlug\":\"my-show\",\"certification\":\"TV-14\",\"genres\":[\"Action\",\"Animation\",\"Comedy\"],\"tags\":[],\"added\":\"2018-01-01T07:40:01.350000Z\",\"ratings\":{\"votes\":0,\"value\":0},\"qualityProfileId\":7,\"id\":58},\"episodes\":[{\"seriesId\":58,\"episodeFileId\":1901,\"seasonNumber\":1,\"episodeNumber\":10,\"title\":\"This Episode (Part 2)\",\"airDate\":\"2018-09-01\",\"airDateUtc\":\"2018-09-01T15:00:00Z\",\"overview\":\"\\\"This is my show!\\\" What kind of show is it?\",\"hasFile\":true,\"monitored\":true,\"absoluteEpisodeNumber\":10,\"sceneAbsoluteEpisodeNumber\":10,\"sceneEpisodeNumber\":10,\"sceneSeasonNumber\":1,\"unverifiedSceneNumbering\":false,\"id\":2714}]}";
            var a = SonarrParsed.FromJson(json);
            Assert.AreEqual(1, a.Episodes.Count);
            Assert.AreEqual("My Show", a.ParsedEpisodeInfo.SeriesTitle);
        }

        [TestMethod]
        public void Radarr()
        {
            const string json = "[{\"title\":\"My Movie\",\"alternativeTitles\":[{\"sourceType\":\"tmdb\",\"movieId\":16,\"title\":\"Movie\",\"sourceId\":123000,\"votes\":0,\"voteCount\":0,\"language\":\"english\",\"id\":46},{\"sourceType\":\"tmdb\",\"movieId\":16,\"title\":\"Please Reconsider\",\"sourceId\":123400,\"votes\":0,\"voteCount\":0,\"language\":\"english\",\"id\":44},{\"sourceType\":\"tmdb\",\"movieId\":16,\"title\":\"Der Mov\",\"sourceId\":123450,\"votes\":0,\"voteCount\":0,\"language\":\"english\",\"id\":41}],\"secondaryYearSourceId\":0,\"sortTitle\":\"my movie\",\"sizeOnDisk\":1340000001,\"status\":\"released\",\"overview\":\"I made a movie!\",\"inCinemas\":\"2019-01-25T00:00:00Z\",\"physicalRelease\":\"2019-02-14T00:00:00Z\",\"images\":[{\"coverType\":\"poster\",\"url\":\"/radarr/MediaCover/1/poster.jpg\"},{\"coverType\":\"fanart\",\"url\":\"/radarr/MediaCover/1/fanart.jpg\"}],\"website\":\"\",\"downloaded\":true,\"year\":2019,\"hasFile\":true,\"studio\":\"Movie Makers\",\"path\":\"/movies/My Movie (2019)\",\"profileId\":2,\"pathState\":\"static\",\"monitored\":true,\"minimumAvailability\":\"announced\",\"isAvailable\":true,\"folderName\":\"/movies/My Movie (2019)\",\"runtime\":137,\"lastInfoSync\":\"2019-02-13T06:24:28.540159Z\",\"cleanTitle\":\"mymovie\",\"imdbId\":\"tt3315342\",\"tmdbId\":263115,\"titleSlug\":\"mymovie-264001\",\"genres\":[],\"tags\":[],\"added\":\"2019-02-01T21:54:43.421033Z\",\"ratings\":{\"votes\":12001,\"value\":7.2},\"movieFile\":{\"movieId\":0,\"relativePath\":\"MyMovie.2019.mp4\",\"size\":1340000001,\"dateAdded\":\"2019-02-01T21:54:43.421033Z\",\"quality\":{\"quality\":{\"id\":6,\"name\":\"Bluray-720p\",\"source\":\"bluray\",\"resolution\":\"r720P\",\"modifier\":\"none\"},\"customFormats\":[],\"revision\":{\"version\":1,\"real\":0}},\"edition\":\"\",\"mediaInfo\":{\"containerFormat\":\"MPEG-4\",\"videoFormat\":\"AVC\",\"videoCodecID\":\"avc1\",\"videoProfile\":\"High@L4\",\"videoCodecLibrary\":\"x264 - core 133 r2334 a3ac64b\",\"videoBitrate\":1233000,\"videoBitDepth\":8,\"videoMultiViewCount\":0,\"videoColourPrimaries\":\"\",\"videoTransferCharacteristics\":\"\",\"width\":1280,\"height\":536,\"audioFormat\":\"AAC\",\"audioCodecID\":\"40\",\"audioCodecLibrary\":\"\",\"audioAdditionalFeatures\":\"\",\"audioBitrate\":63001,\"runTime\":\"01:30:05.2680000\",\"audioStreamCount\":1,\"audioChannels\":2,\"audioChannelPositions\":\"2/0/0\",\"audioChannelPositionsText\":\"Front: L R\",\"audioProfile\":\"HE-AAC\",\"videoFps\":23.976,\"audioLanguages\":\"English\",\"subtitles\":\"\",\"scanType\":\"Progressive\",\"schemaRevision\":5},\"id\":12},\"qualityProfileId\":2,\"id\":16},{\"title\":\"Assassin's Creed\",\"sortTitle\":\"assassins creed\",\"sizeOnDisk\":0,\"status\":\"released\",\"overview\":\"Lynch discovers he is a descendant of the secret Assassins society through unlocked genetic memories that allow him to relive the adventures of his ancestor, Aguilar, in 15th Century Spain. After gaining incredible knowledge and skills he�s poised to take on the oppressive Knights Templar in the present day.\",\"inCinemas\":\"2016-12-21T00:00:00Z\",\"images\":[{\"coverType\":\"poster\",\"url\":\"/radarr/MediaCover/1/poster.jpg?lastWrite=636200219330000000\"},{\"coverType\":\"banner\",\"url\":\"/radarr/MediaCover/1/banner.jpg?lastWrite=636200219340000000\"}],\"website\":\"https://www.ubisoft.com/en-US/\",\"downloaded\":false,\"year\":2016,\"hasFile\":false,\"youTubeTrailerId\":\"pgALJgMjXN4\",\"studio\":\"20th Century Fox\",\"path\":\"/path/to/Assassin's Creed (2016)\",\"profileId\":6,\"monitored\":true,\"minimumAvailability\":\"preDB\",\"runtime\":115,\"lastInfoSync\":\"2017-01-23T22:05:32.365337Z\",\"cleanTitle\":\"assassinscreed\",\"imdbId\":\"tt2094766\",\"tmdbId\":121856,\"titleSlug\":\"assassins-creed-121856\",\"genres\":[\"Action\",\"Adventure\",\"Fantasy\",\"Science Fiction\"],\"tags\":[],\"added\":\"2017-01-14T20:18:52.938244Z\",\"ratings\":{\"votes\":711,\"value\":5.2},\"qualityProfileId\":6,\"id\":1}]";
            var movies = Movie.FromJson(json);
            Assert.AreEqual(2, movies.Count);
            Assert.AreEqual("mymovie", movies[0].CleanTitle);
            Assert.AreEqual("tt2094766", movies[1].ImdbId);
        }
    }
}
