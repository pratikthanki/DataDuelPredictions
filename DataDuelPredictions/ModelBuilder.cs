using System;
using System.Diagnostics;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.FastTree;
using Microsoft.ML.Trainers.FastTree;

namespace DataDuelPredictions
{
    class ModelBuilder
    {
        private static MLContext mlContext;
        private static readonly string RelativePath = @"../../../../DataDuelPredictions";
        private static readonly string DataRelativePath = $"{RelativePath}/results.csv";
        private static readonly string DataPath = GetAbsolutePath(DataRelativePath);

        public ModelBuilder()
        {
            mlContext = new MLContext();
        }

        public ITransformer BuildModel()
        {
            // Load data from CSV and parse into mlContext data object
            var transformedCsv = CsvReader.GetData(DataPath);
            var dataView = mlContext.Data.LoadFromEnumerable(transformedCsv);

            // Split into train-test
            var dataSplit = mlContext.Data.TrainTestSplit(dataView, 0.2);
            var trainData = dataSplit.TrainSet;
            var testData = dataSplit.TestSet;

            // Transform and train
            var trainedModel = Train(trainData);

            Evaluate(trainedModel, testData);

            return trainedModel;
        }

        private ITransformer Train(IDataView trainData)
        {
            var options = new FastForestRegressionTrainer.Options()
            {
                NumberOfTrees = 500,
                MinimumExampleCountPerLeaf = 100
            };

            // Data process configuration with pipeline data transformations
            var trainingPipeline = mlContext.Transforms
                .CopyColumns("Label", nameof(Match.FullTimeGoals))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(
                    "TeamEncoded", nameof(Match.Team)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(
                    "OpponentEncoded", nameof(Match.Opponent)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(
                    "IsHomeEncoded", nameof(Match.IsHome)))
                .Append(mlContext.Transforms.Concatenate(
                    "Features", "IsHomeEncoded", "TeamEncoded", "OpponentEncoded"))
                .Append(mlContext.Regression.Trainers.FastForest(options));

            var model = trainingPipeline.Fit(trainData);

            return model;
        }

        private void Evaluate(ITransformer trainedModel, IDataView testData)
        {
            var predictions = trainedModel.Transform(testData);
            var metrics =
                mlContext.Regression.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");

            Console.WriteLine($"**ModelBuilder metrics**");
            Console.WriteLine($"Loss Function            : {metrics.LossFunction}");
            Console.WriteLine($"R Squared                : {metrics.RSquared}");
            Console.WriteLine($"Mean Absolute Error      : {metrics.MeanAbsoluteError}");
            Console.WriteLine($"Mean Squared Error       : {metrics.MeanSquaredError}");
            Console.WriteLine($"Root Mean Squared Error  : {metrics.RootMeanSquaredError}");

        }

        public int PredictionMatchScore(ITransformer trainedModel, Match match)
        {
            var predictionFunction =
                mlContext.Model.CreatePredictionEngine<Match, FullTimeScore>(trainedModel);

            var prediction = predictionFunction.Predict(match);

            // Aggressive rounding to avoid predicting every score with 0's and 1's
            return (int) Math.Floor(prediction.FullTimeGoals + (double) 0.6m);
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