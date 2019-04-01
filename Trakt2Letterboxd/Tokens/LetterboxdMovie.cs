using System.Collections.Generic;
using System.Linq;
using CsvHelper.Configuration.Attributes;
using Newtonsoft.Json;

namespace Trakt2Letterboxd.Tokens
{
    public class LetterboxdMovie
    {
        [Name("WatchedDate")]        
        public string WatchedDate { get; set; }

        [Name("Year")]
        public int Year { get; set; }

        [Name("Title")]
        public string Title { get; set; }

        [Name("tmdbID")]
        public int? TmdbId { get; set; }

        [Name("imdbID")]
        public string ImdbId { get; set; }

        [Name("Rating10")]
        public int? Rating10 { get; set; }

        public LetterboxdMovie(TraktHistory history, 
            IEnumerable<TraktHistory> ratings)
        {
            if (history == null)            
                throw new System.ArgumentNullException(nameof(history));
            
            WatchedDate = history.WatchedAt;
            Year = history.Movie.Year;
            Title = history.Movie.Title;
            TmdbId = history.Movie.Ids.Tmdb;
            ImdbId = history.Movie.Ids.Imdb;            

            if (ratings != null)
            {
                var ex = ratings.FirstOrDefault(x => x.Movie.Ids.Trakt == history.Movie.Ids.Trakt);

                if (ex != null)
                    Rating10 = ex.Rating;
            }
        }
    }
}