using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DataDuelPredictions
{
    public class PredictionWriter
    {
        private const string BaseUrl = Settings.BaseUrl;
        private const string ApiKey = Settings.ApiKey;
        private const string ApplicationJson = "application/json";
        private const string Put = "PUT";

        private const int Season = 2020;
        private static readonly string SeasonFixturesEndpoint = $"/api/season/{Season}/fixture";
        private const string PredictionEndpoint = @"/api/prediction";

        public PredictionWriter()
        {
        }

        public static async Task<IList<DataDuelMatch>> GetFixtures(int? matchday)
        {
            var endpoint = SeasonFixturesEndpoint;
            if (matchday != null) 
                endpoint = $"/api/season/{Season}/matchday/{matchday}/fixture";
            
            var request = WebRequest.Create($"{BaseUrl}{endpoint}");

            using var response = (HttpWebResponse) await request.GetResponseAsync();
            await using var dataStream = response.GetResponseStream();
            using var reader = new StreamReader(dataStream!);

            var content = await reader.ReadToEndAsync();
            var responseJson = JsonConvert.DeserializeObject<List<DataDuelMatch>>(content);

            return responseJson;
        }

        public static async Task PutPredictions(List<DataDuelPrediction> predictions)
        {
            var request = (HttpWebRequest) WebRequest.Create($"{BaseUrl}{PredictionEndpoint}");
            var jsonString = JsonConvert.SerializeObject(predictions);

            request.Method = Put;
            request.ContentType = ApplicationJson;
            request.Headers.Add(HttpRequestHeader.Authorization, ApiKey);

            Debug.Assert(predictions.Count > 0);

            await using var dataStream = await request.GetRequestStreamAsync();
            var byteArray = Encoding.UTF8.GetBytes(jsonString);
            var byteArrayLength = byteArray.Length;

            request.ContentLength = byteArrayLength;

            try
            {
                await dataStream.WriteAsync(byteArray, 0, byteArrayLength);
            }
            catch (WebException e)
            {
                Console.WriteLine($"WebException: {e.Status} - {e.Message}");
            }

            var response = (HttpWebResponse) await request.GetResponseAsync();

            if (response.StatusCode != HttpStatusCode.OK)
                Console.WriteLine("Error in updating DataDuel predictions");

            Console.WriteLine("DataDuel predictions updated");
        }
    }
}
