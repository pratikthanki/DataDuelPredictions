namespace DataDuelPredictions
{
    public class DataDuelPut
    {
        public int fixtureId { get; set; }
        public PredictedScore score { get; set; }
    }

    public class PredictedScore
    {
        public int homeGoals { get; set; }
        public int awayGoals { get; set; }
    }
}
