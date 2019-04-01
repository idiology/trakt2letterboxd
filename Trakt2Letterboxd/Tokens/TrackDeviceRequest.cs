using Newtonsoft.Json;

namespace Trakt2Letterboxd.Tokens
{
    public class TrackDeviceRequest
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }

        public TrackDeviceRequest(string clientId, string code, string clientSecret)
        {
            ClientId = clientId;
            Code = code;
            ClientSecret = clientSecret;
        }
    }
}