using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace DataDuelPredictions
{
    internal static class Program
    {
        private static readonly string RelativePath = @"../../../../DataDuelPredictions";
        private static readonly string DataRelativePath = $"{RelativePath}/results.csv";
        private static readonly string DataPath = GetAbsolutePath(DataRelativePath);

        static void Main(string[] args)
        {
            var mlContext = new MLContext(0);

            // Load data from CSV and parse into mlContext data object
            var transformedCsv = CsvReader.GetData(DataPath);
            var dataView = mlContext.Data.LoadFromEnumerable(transformedCsv);

            // Split into train-test
            var dataSplit = mlContext.Data.TrainTestSplit(dataView, 0.2);
            var trainData = dataSplit.TrainSet;
            var testData = dataSplit.TestSet;

            // Transform 
            var trainingPipeline = BuildTrainingPipeline(mlContext);

            // Train trainedModel on training data 
            var trainedModel = trainingPipeline.Fit(trainData);

            var metrics = Evaluate(mlContext, trainedModel, testData);

            // TestSinglePrediction(mlContext, trainedModel);
        }

        private static EstimatorChain<RegressionPredictionTransformer<PoissonRegressionModelParameters>> BuildTrainingPipeline(MLContext mlContext)
        {
            // Data process configuration with pipeline data transformations
            return mlContext.Transforms
                .CopyColumns("Label", nameof(Match.FullTimeGoals))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("TeamEncoded", nameof(Match.Team)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("OpponentEncoded", nameof(Match.Opponent)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("IsHomeEncoded", nameof(Match.IsHome)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("FullTimeGoalsEncoded",
                    nameof(Match.FullTimeGoals)))
                .Append(mlContext.Transforms.Concatenate("Features",
                    "FullTimeGoalsEncoded", "IsHomeEncoded", "TeamEncoded", "OpponentEncoded"))
                .Append(mlContext.Regression.Trainers.LbfgsPoissonRegression());
        }

        private static RegressionMetrics Evaluate(MLContext mlContext, ITransformer trainedModel, IDataView testData)
        {
            var predictions = trainedModel.Transform(testData);
            var metrics =
                mlContext.Regression.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");

            Console.WriteLine($"**Model metrics**");
            Console.WriteLine($"Loss Function            : {metrics.LossFunction}");
            Console.WriteLine($"R Squared                : {metrics.RSquared}");
            Console.WriteLine($"Mean Absolute Error      : {metrics.MeanAbsoluteError}");
            Console.WriteLine($"Mean Squared Error       : {metrics.MeanSquaredError}");
            Console.WriteLine($"Root Mean Squared Error  : {metrics.RootMeanSquaredError}");

            return metrics;
        }

        private static void TestSinglePrediction(MLContext mlContext, ITransformer model)
        {
            var predictionFunction =
                mlContext.Model.CreatePredictionEngine<Match, ScorePrediction>(model);

            var matchSample = new Match()
            {
                MatchDate = DateTime.ParseExact("08/03/2020", "dd/MM/yyyy", null),
                Team = "Man United",
                Opponent = "Man City",
                FullTimeGoals = 2,
                IsHome = 1
            };
            
            var prediction = predictionFunction.Predict(matchSample);
            
            Console.WriteLine($"**********************************************************************");
            Console.WriteLine($"Predicted fare: {prediction.FullTimeGoals}, actual: 2");
            Console.WriteLine($"**********************************************************************");

        }

        private static string GetAbsolutePath(string relativePath)
        {
            var _dataRoot = new FileInfo(typeof(Program).Assembly.Location);
            var assemblyFolderPath = _dataRoot.Directory.FullName;

            Debug.Assert(_dataRoot != null);
            var fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }
    }

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
