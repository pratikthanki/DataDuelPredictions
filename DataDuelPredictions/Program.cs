using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DataDuelPredictions
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            // Set to null to get and predict all matchday's
            int? matchday = null;
            var matchdayFixtures = await PredictionWriter.GetFixtures(matchday);

            // Only care about matches yet to be played  
            const string Pending = "Pending";
            var matches = matchdayFixtures
                .SelectMany(week => week.fixtures.Where(game => game.status == Pending))
                .Select(match => new[]
                    {
                        new Match
                        {
                            FixtureId = match.id,
                            MatchDate = match.date,
                            Team = match.homeTeam.name,
                            Opponent = match.awayTeam.name,
                            FullTimeGoals = (float) 0,
                            IsHome = (float) 1
                        },
                        new Match
                        {
                            FixtureId = match.id,
                            MatchDate = match.date,
                            Team = match.awayTeam.name,
                            Opponent = match.homeTeam.name,
                            FullTimeGoals = (float) 0,
                            IsHome = (float) 0
                        }
                    }
                ).ToList();

            var predictions = PredictScores(matches);
            await PredictionWriter.PutPredictions(predictions);
        }

        private static List<DataDuelPrediction> PredictScores(IEnumerable<Match[]> matches)
        {
            var modelBuilder = new ModelBuilder();
            var trainedModel = modelBuilder.BuildModel();

            var predictions = new List<DataDuelPrediction>();

            foreach (var match in matches)
            {
                var homeTeam = match.First(x => x.IsHome == (float) 1);
                var awayTeam = match.First(x => x.IsHome == (float) 0);

                Debug.Assert(homeTeam.FixtureId == awayTeam.FixtureId);

                var prediction = new DataDuelPrediction
                {
                    fixtureId = homeTeam.FixtureId,
                    score = new PredictedScore
                    {
                        homeGoals = modelBuilder.PredictionMatchScore(trainedModel, homeTeam),
                        awayGoals = modelBuilder.PredictionMatchScore(trainedModel, awayTeam)
                    }
                };

                predictions.Add(prediction);
                Console.WriteLine(
                    $"{homeTeam.Team} ({prediction.score.homeGoals}) : {awayTeam.Team} ({prediction.score.awayGoals})");
            }

            return predictions;
        }
    }
}
