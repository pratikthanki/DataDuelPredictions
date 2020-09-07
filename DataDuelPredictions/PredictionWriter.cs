using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DataDuelPredictions
{
    public class PredictionWriter
    {
        private const string BaseUrl = Settings.BaseUrl;
        private const string ApiKey = Settings.ApiKey;
        private const string ApplicationJson = "application/json";

        private const int Season = 2020;
        private readonly string MatchDayEndpoint = $"/api/season/{Season}/matchday/1/fixture";
        private readonly string FixturesEndpoint = $"/api/season/{Season}/fixture";
        private const string PredictionEndpoint = @"/api/prediction";

        public PredictionWriter()
        {
        }

        public async Task<DataDuelMatch> GetFixtures()
        {
            var request = WebRequest.Create($"{BaseUrl}{FixturesEndpoint}");

            using var response = (HttpWebResponse) await request.GetResponseAsync();
            await using var dataStream = response.GetResponseStream();
            using var reader = new StreamReader(dataStream!);

            var content = await reader.ReadToEndAsync();
            var responseJson = JsonConvert.DeserializeObject<DataDuelMatch>(content);

            return responseJson;
        }

        public async Task PutPredictions(List<ScorePrediction> predictions)
        {
            var request = WebRequest.Create($"{BaseUrl}{PredictionEndpoint}");

            request.Method = "PUT";
            request.ContentType = ApplicationJson;
            request.Headers.Add(HttpRequestHeader.Authorization, ApiKey);

            Debug.Assert(predictions.Count > 0);

            await using var dataStream = await request.GetRequestStreamAsync();
            var ser = new DataContractSerializer(predictions.GetType());
            ser.WriteObject(dataStream, predictions);

            var response = (HttpWebResponse) await request.GetResponseAsync();
            var returnString = response.StatusCode.ToString();
        }
    }
}
