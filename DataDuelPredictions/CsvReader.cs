using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataDuelPredictions
{
    public static class CsvReader
    {
        public static IEnumerable<Match> GetData(string filePath)
        {
            var results =
                File.ReadAllLines(filePath)
                    .Skip(1)
                    .Select(x => x.Split(","))
                    .Select(x => new CsvSchema
                    {
                        MatchDate = DateTime.ParseExact(x[0], "dd/MM/yyyy", null),
                        HomeTeam = x[1],
                        AwayTeam = x[2],
                        FullTimeHomeTeamGoals = int.Parse(x[3]),
                        FullTimeAwayTeamGoals = int.Parse(x[4])
                    });

            return TransformResults(results);
        }

        private static IEnumerable<Match> TransformResults(IEnumerable<CsvSchema> results)
        {
            var matchResults = results.ToList();
            var transformed = matchResults
                .Select(x => new[]
                    {
                        new Match
                        {
                            MatchDate = x.MatchDate,
                            Team = x.HomeTeam,
                            Opponent = x.AwayTeam,
                            FullTimeGoals = (float) x.FullTimeHomeTeamGoals,
                            IsHome = (float) 1
                        },
                        new Match
                        {
                            MatchDate = x.MatchDate,
                            Team = x.AwayTeam,
                            Opponent = x.HomeTeam,
                            FullTimeGoals = (float) x.FullTimeAwayTeamGoals,
                            IsHome = (float) 0
                        }
                    }
                ).SelectMany(o => o)
                .ToList();

            return transformed.ToList();
        }
    }
}
