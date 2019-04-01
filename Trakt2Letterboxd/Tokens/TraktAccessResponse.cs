using Newtonsoft.Json;

namespace Trakt2Letterboxd.Tokens
{
    public class TraktAccessResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}