using Newtonsoft.Json;

namespace Trakt2Letterboxd.Tokens
{
    public class TraktDeviceResponse
    {
        [JsonProperty("device_code")]
        public string DeviceCode { get; set; }

        [JsonProperty("user_code")]
        public string UserCode { get; set; }

        [JsonProperty("verification_url")]
        public string VerificationUrl { get; set; }        

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }        

        [JsonProperty("interval")]
        public int Interval { get; set; }        
    }
}