using Newtonsoft.Json;

namespace Trakt2Letterboxd.Tokens
{
    public class TraktClientIdRequest
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        public TraktClientIdRequest(string clientId)
        {
            ClientId = clientId;
        }
    }
}