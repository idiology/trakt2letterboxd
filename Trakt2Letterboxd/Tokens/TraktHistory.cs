using Newtonsoft.Json;

namespace Trakt2Letterboxd.Tokens
{
    public class TraktHistory
    {
        [JsonProperty("watched_at")]
        public string WatchedAt { get; set; }

        public int Rating { get; set; }
        
        public TraktMovie Movie { get; set; }
    }
}