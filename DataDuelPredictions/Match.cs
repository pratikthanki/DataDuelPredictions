using System;
using Microsoft.ML.Data;

namespace DataDuelPredictions
{
    public class Match
    {
        public int FixtureId;
        public DateTime MatchDate;
        public string Team;
        public string Opponent;
        public float FullTimeGoals;
        public float IsHome;
    }

    public class FullTimeScore
    {
        [ColumnName("Score")] public float FullTimeGoals;
    }
}
