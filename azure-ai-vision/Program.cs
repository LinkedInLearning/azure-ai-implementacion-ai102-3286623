using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string endpoint = config["Endpoint"];
string key = config["Key"];

var client = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));

ImageAnalysisResult result = await client.AnalyzeAsync(
    new Uri("https://faburobotics.com/media/photos/landing.png"),
    VisualFeatures.Caption | VisualFeatures.Tags);

Console.WriteLine($"Image analysis results:");
Console.WriteLine($"Caption:");
Console.WriteLine($"'{result.Caption.Text}', Confidence {result.Caption.Confidence:F4}");
foreach (var t in result.Tags.Values)
{
    Console.WriteLine($"{t.Name} {t.Confidence}");
}