using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataDuelPredictions
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            var Pending = "Pending";
            // var model = new Model();
            var games = await PredictionWriter.GetFixtures();


            var matches = games
                .SelectMany(week => week.fixtures.Where(game => game.status == Pending))
                .Select(match => new[]
                    {
                        new Match
                        {
                            MatchDate = match.date,
                            Team = match.homeTeam.name,
                            Opponent = match.awayTeam.name,
                            FullTimeGoals = (float) 0,
                            IsHome = (float) 1
                        },
                        new Match
                        {
                            MatchDate = match.date,
                            Team = match.awayTeam.name,
                            Opponent = match.homeTeam.name,
                            FullTimeGoals = (float) 0,
                            IsHome = (float) 0
                        }
                    }
                ).SelectMany(o => o)
                .ToList();
        }
    }
}
