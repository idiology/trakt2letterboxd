using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using ShellProgressBar;
using Console = Colorful.Console;

namespace Trakt2Letterboxd
{
    public class Trakt
    {
        const string TokenFile = ".token";
        const string PaginationHeader = "X-Pagination-Page-Count";
        const int ListPageSize = 10;
        readonly string _apiRoot;
        readonly string _clientId;
        readonly string _clientSecret;

        Tokens.TraktClientIdRequest CurrentClientIdRequest => new Tokens.TraktClientIdRequest(_clientId);

        public Trakt(string apiRoot, string clientId, string clientSecret)
        {
            _clientSecret = clientSecret;
            _clientId = clientId;
            _apiRoot = apiRoot;
        }

        public async Task<string> GetAuthTokenAsync()
        {
            var token = await GetAuthTokenFromFileAsync();

            if (token != null)
                return token;

            var device = await GetDeviceTokenAsync().ConfigureAwait(false);
            var over = DateTime.Now.AddSeconds(device.ExpiresIn);

            Console.Write("Go to ");
            Console.Write(device.VerificationUrl, Color.Yellow);
            Console.Write(" and enter this user code there: ");
            Console.WriteLine(device.UserCode, Color.Pink);

            var totalTicks = device.ExpiresIn / device.Interval;

            var options = new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ForegroundColor = ConsoleColor.DarkRed,
                DisplayTimeInRealTime = true,
                CollapseWhenFinished = true
            };

            var deviceRequest = new Tokens.TrackDeviceRequest(_clientId, device.DeviceCode, _clientSecret);

            using (var bar = new ProgressBar(totalTicks, "Waiting for confirmation", options))
            {
                while (DateTime.Now < over)
                {
                    Thread.Sleep(device.Interval * 1000);
                    bar.Tick();

                    var result = await _apiRoot
                                .AllowHttpStatus(HttpStatusCode.BadRequest)
                                .AppendPathSegment("oauth")
                                .AppendPathSegment("device")
                                .AppendPathSegment("token")
                                .PostJsonAsync(deviceRequest)
                                .ReceiveJson<Tokens.TraktAccessResponse>()
                                .ConfigureAwait(false);

                    if (result != null)
                    {
                        Console.ResetColor();
                        Console.WriteLine("\r\n\r\nAuthorized!", Color.Green);

                        await SetAuthTokenInFileAsync(result.AccessToken);

                        return result.AccessToken;
                    }
                }
            }

            Console.WriteLine("Authorization failed.", Color.Red);
            return null;
        }

        Task<HttpResponseMessage> RequestMoviesAsync(string token, string list, int page, int limit)
        {
            return _apiRoot
                .AppendPathSegment("sync")
                .AppendPathSegment(list)
                .AppendPathSegment("movies")
                .WithHeader("Content-type", "application/json")
                .WithHeader("trakt-api-key", _clientId)
                .WithHeader("Authorization", $"Bearer {token}")
                .SetQueryParam("page", page)
                .SetQueryParam("limit", limit)
                .GetAsync();
        }

        public async Task<IEnumerable<Tokens.LetterboxdMovie>> GetMoviesAsync(string token, string list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (token == null)
                throw new ArgumentNullException(nameof(token));

            var movies = new List<Tokens.TraktHistory>();

            HttpResponseMessage all;

            try
            {
                all = await RequestMoviesAsync(token, list, 1, 1).ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {                
                if (ex.Call.HttpStatus == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("Auth token is not valid.", Color.Red);

                    if (File.Exists(TokenFile))
                        File.Delete(TokenFile);                    
                }

                return null;
            }

            var numberOfMovies = 0;
            IEnumerable<string> numberOfMoviesRaw;

            if (!all.Headers.TryGetValues(PaginationHeader, out numberOfMoviesRaw))
            {
                Console.WriteLine($"Unable to determine a number of items in ${list} list.");
                return null;
            }

            if (!Int32.TryParse(numberOfMoviesRaw.First(), out numberOfMovies))
            {
                Console.WriteLine($"Unable to determine a number of items in ${list} list.");
                return null;
            }

            var options = new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ForegroundColor = ConsoleColor.Magenta,
                DisplayTimeInRealTime = true,
                CollapseWhenFinished = true
            };

            var numberOfPages = (numberOfMovies / ListPageSize) + (numberOfMovies % ListPageSize == 0 ? 0 : 1);
            var page = 1;

            using (var bar = new ProgressBar(numberOfPages, $"Grabbing movies from {list}, page {page} / {numberOfPages}", options))
            {
                while (page <= numberOfPages)
                {
                    var result = await RequestMoviesAsync(token, list, page, ListPageSize).ReceiveJson<IEnumerable<Tokens.TraktHistory>>();

                    movies.AddRange(result);

                    bar.Tick($"Grabbing movies from {list}, page {page} / {numberOfPages}");
                    page++;
                }
            }

            Console.Write("Movies grabbed: ");
            Console.WriteLine(movies.Count(), Color.Green);

            var ratings = await _apiRoot
                .AppendPathSegment("sync")
                .AppendPathSegment("ratings")
                .AppendPathSegment("movies")
                .WithHeader("Content-type", "application/json")
                .WithHeader("trakt-api-key", _clientId)
                .WithHeader("Authorization", $"Bearer {token}")
                .GetJsonAsync<IEnumerable<Tokens.TraktHistory>>()
                .ConfigureAwait(false);

            Console.Write("Ratings grabbed: ");
            Console.WriteLine(ratings.Count(), Color.Green);

            var letterMovies = new List<Tokens.LetterboxdMovie>();

            foreach (var m in movies)
            {
                letterMovies.Add(new Tokens.LetterboxdMovie(m, ratings));
            }

            Console.Write("Ratings synced: ");
            Console.WriteLine(letterMovies.Count(x => x.Rating10.HasValue), Color.Green);

            return letterMovies;
        }

        static async Task<string> GetAuthTokenFromFileAsync()
        {
            if (!File.Exists(TokenFile))
                return null;

            var token = await File.ReadAllTextAsync(TokenFile);

            if (string.IsNullOrWhiteSpace(token))
                return null;

            return token;
        }

        static Task SetAuthTokenInFileAsync(string code)
        {
            return File.WriteAllTextAsync(TokenFile, code);
        }

        Task<Tokens.TraktDeviceResponse> GetDeviceTokenAsync()
        {
            return _apiRoot
                .AppendPathSegment("oauth")
                .AppendPathSegment("device")
                .AppendPathSegment("code")
                .WithHeader("Content-type", "application/json")
                .PostJsonAsync(CurrentClientIdRequest)
                .ReceiveJson<Tokens.TraktDeviceResponse>();
        }
    }
}