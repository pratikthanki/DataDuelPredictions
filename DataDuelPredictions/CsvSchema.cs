using System;
using Microsoft.ML.Data;

namespace DataDuelPredictions
{
    public class CsvSchema
    {
        [LoadColumn(0)] public DateTime MatchDate;

        [LoadColumn(1)] public string HomeTeam;

        [LoadColumn(2)] public string AwayTeam;

        [LoadColumn(3)] public int FullTimeHomeTeamGoals;

        [LoadColumn(4)] public int FullTimeAwayTeamGoals;
    }
}