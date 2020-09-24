using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
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
            var data = mlContext.Data.LoadFromEnumerable(transformedCsv);

            // Split into train-test
            var dataSplit = mlContext.Data.TrainTestSplit(data, 0.2);
            var trainData = dataSplit.TrainSet;
            var testData = dataSplit.TestSet;

            // Transform and train
            var trainingPipeline = BuildPipeline();
            var trainedModel = trainingPipeline.Fit(trainData);

            Evaluate(trainedModel, testData);
            CrossValidate(data, trainingPipeline);

            return trainedModel;
        }

        private static EstimatorChain<RegressionPredictionTransformer<FastForestRegressionModelParameters>> BuildPipeline()
        {
            var options = new FastForestRegressionTrainer.Options()
            {
                NumberOfTrees = 500,
                MinimumExampleCountPerLeaf = 100
            };

            // Data process configuration with pipeline data transformations
            var trainingPipeline =
                mlContext.Transforms.Categorical
                    .OneHotEncoding("TeamEncoded", nameof(Match.Team))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(
                        "MatchDateEncoded", nameof(Match.MatchDate)))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(
                        "OpponentEncoded", nameof(Match.Opponent)))
                    .Append(mlContext.Transforms.Concatenate(
                        "Features",
                        "IsHome", "TeamEncoded", "OpponentEncoded", "MatchDateEncoded"))
                    .Append(mlContext.Regression.Trainers.FastForest(options));

            return trainingPipeline;
        }

        private static void Evaluate(ITransformer trainedModel, IDataView testData)
        {
            var predictions = trainedModel.Transform(testData);
            var metrics = mlContext.Regression.Evaluate(predictions);

            Console.WriteLine($"**ModelBuilder metrics**");
            Console.WriteLine($"Loss Function            : {metrics.LossFunction:0.###}");
            Console.WriteLine($"R Squared                : {metrics.RSquared:0.###}");
            Console.WriteLine($"Mean Absolute Error      : {metrics.MeanAbsoluteError:0.###}");
            Console.WriteLine($"Mean Squared Error       : {metrics.MeanSquaredError:0.###}");
            Console.WriteLine($"Root Mean Squared Error  : {metrics.RootMeanSquaredError:0.###}");

        }

        private static void CrossValidate(
            IDataView data,
            EstimatorChain<RegressionPredictionTransformer<FastForestRegressionModelParameters>> pipeline)
        {
            // Evaluate the model again using cross-validation
            var scores = mlContext.Regression.CrossValidate(data, pipeline, 10);

            var lossFunction = scores.Average(x => x.Metrics.LossFunction);
            var rSquared = scores.Average(x => x.Metrics.RSquared);
            var meanAbsoluteError = scores.Average(x => x.Metrics.MeanAbsoluteError);
            var meanSquaredError = scores.Average(x => x.Metrics.MeanSquaredError);
            var rootMeanSquaredError = scores.Average(x => x.Metrics.RootMeanSquaredError);

            Console.WriteLine($"**ModelBuilder CV metrics**");
            Console.WriteLine($"Loss Function            : {lossFunction:0.###}");
            Console.WriteLine($"R Squared                : {rSquared:0.###}");
            Console.WriteLine($"Mean Absolute Error      : {meanAbsoluteError:0.###}");
            Console.WriteLine($"Mean Squared Error       : {meanSquaredError:0.###}");
            Console.WriteLine($"Root Mean Squared Error  : {rootMeanSquaredError:0.###}");

        }

        public static int PredictMatchScore(ITransformer trainedModel, Match match)
        {
            var predictionFunction =
                mlContext.Model.CreatePredictionEngine<Match, FullTimeScore>(trainedModel);

            var prediction = predictionFunction.Predict(match);

            // Aggressive rounding to avoid predicting every score with 0's and 1's
            return (int) Math.Floor(prediction.FullTimeGoals + (double) 0.5m);
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