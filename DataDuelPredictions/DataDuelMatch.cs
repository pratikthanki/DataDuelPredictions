using System;
using System.Collections.Generic;

namespace DataDuelPredictions
{
    public class DataDuelMatch
    {
        public int matchDay { get; set; }
        public IList<Fixture> fixtures { get; set; }
    }

    public class Fixture
    {
        public int id { get; set; }
        public DateTime date { get; set; }
        public HomeTeam homeTeam { get; set; }
        public AwayTeam awayTeam { get; set; }
        public Score score { get; set; }
        public object predictedScore { get; set; }
        public bool canEdit { get; set; }
        public string status { get; set; }
        public int awardedPoints { get; set; }
        public string result { get; set; }
        public string predictedResult { get; set; }
    }

    public class HomeTeam
    {
        public int id { get; set; }
        public object logoUrl { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string code { get; set; }
    }

    public class AwayTeam
    {
        public int id { get; set; }
        public object logoUrl { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string code { get; set; }
    }

    public class Score
    {
        public int? homeGoals { get; set; }
        public int? awayGoals { get; set; }
    }
}