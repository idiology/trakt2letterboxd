using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Console = Colorful.Console;

namespace Trakt2Letterboxd
{
    class Program
    {
        const string ApiRoot = "https://api.trakt.tv";
        const string ClientId = "bf2c554441f842b64062e81f162dd3b14305deee27ff907ed21cc23d46cd55a8";
        const string ClientSecret = "32b4674dee949d05d0e90c07318dab650b6c9cbd851c717ff2d32ed44165be11";
        const string OutputFile = "trakt-exported-history.csv";

        static string _token = null;

        static async Task Main(string[] args)
        {
            Console.WriteAscii("Trakt2Letterboxd", Color.Blue);

            var trakt = new Trakt(ApiRoot, ClientId, ClientSecret);
            IEnumerable<Tokens.LetterboxdMovie> movies;

            while (true)
            {
                _token = await trakt.GetAuthTokenAsync().ConfigureAwait(false);

                if (_token == null)
                {
                    Console.WriteLine("Getting auth token failed.", Color.Red);
                    return;
                }

                movies = await trakt.GetMoviesAsync(_token, "history").ConfigureAwait(false);

                if (movies != null)
                    break;
            }

            if (movies.Any())
            {
                using (var writer = new StreamWriter(OutputFile))
                {
                    using (var csv = new CsvWriter(writer))
                    {
                        csv.Configuration.Delimiter = ",";
                        csv.WriteRecords(movies);
                    }
                }

                Console.Write("Export file ");
                Console.Write(OutputFile, Color.Pink);
                Console.WriteLine(" created.");
            }
            else
            {
                Console.WriteLine("Nothing to export.");
            }

            Console.ResetColor();
        }
    }
}
