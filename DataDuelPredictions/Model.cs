using System;
using System.Diagnostics;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace DataDuelPredictions
{
    class Model
    {
        private static readonly string RelativePath = @"../../../../DataDuelPredictions";
        private static readonly string DataRelativePath = $"{RelativePath}/results.csv";
        private static readonly string DataPath = GetAbsolutePath(DataRelativePath);

        public Model()
        {
        }

        public void BuildModel()
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

            Evaluate(mlContext, trainedModel, testData);

            // TestSinglePrediction(mlContext, trainedModel);
        }

        private static EstimatorChain<RegressionPredictionTransformer<PoissonRegressionModelParameters>>
            BuildTrainingPipeline(MLContext mlContext)
        {
            // Data process configuration with pipeline data transformations
            return mlContext.Transforms
                .CopyColumns("Label", nameof(Match.FullTimeGoals))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("TeamEncoded", nameof(Match.Team)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("OpponentEncoded", nameof(Match.Opponent)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("IsHomeEncoded", nameof(Match.IsHome)))
                .Append(mlContext.Transforms.Concatenate("Features", "IsHomeEncoded", "TeamEncoded", "OpponentEncoded"))
                .Append(mlContext.Regression.Trainers.LbfgsPoissonRegression());
        }

        private static void Evaluate(MLContext mlContext, ITransformer trainedModel, IDataView testData)
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

        }

        private static void MakePrediction(MLContext mlContext, ITransformer model)
        {
            var predictionFunction =
                mlContext.Model.CreatePredictionEngine<Match, FullTimeScore>(model);

            var matchSample = new Match
            {
                MatchDate = DateTime.ParseExact("08/03/2020", "dd/MM/yyyy", null),
                Team = "Man United",
                Opponent = "Man City",
                FullTimeGoals = 2,
                IsHome = 1
            };

            var prediction = predictionFunction.Predict(matchSample);
        }

        private static string GetAbsolutePath(string relativePath)
        {
            var _dataRoot = new FileInfo(typeof(Program).Assembly.Location);

            Debug.Assert(_dataRoot.Directory != null);
            var assemblyFolderPath = _dataRoot.Directory.FullName;

            var fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }
    }
}
