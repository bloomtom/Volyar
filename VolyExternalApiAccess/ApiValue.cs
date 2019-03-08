using System;
using System.Collections.Generic;
using System.Text;

namespace VolyExternalApiAccess
{
    public class ApiValue
    {
        public string SeriesTitle { get; set; } = null;
        public string Title { get; set; } = null;
        public string ImdbId { get; set; } = null;
        public int? TmdbId { get; set; } = null;
        public int? TvdbId { get; set; } = null;
        public int? TvMazeId { get; set; } = null;
        public int EpisodeNumber { get; set; } = 0;
        public int SeasonNumber { get; set; } = 0;
        public int AbsoluteEpisodeNumber { get; set; } = 0;
        public List<string> Genres { get; set; } = null;
    }
}
