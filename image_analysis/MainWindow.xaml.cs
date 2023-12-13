using Azure;
using Azure.AI.Vision.Common;
using Azure.AI.Vision.ImageAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace image_analysis;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        MyCanvas.Children.Clear();
        ContentText.Text = string.Empty;
        LoadImageFromUrl(new Uri(ImageUrl.Text));
    }

    private void AnalyzeImage_Click(object sender, RoutedEventArgs e)
    {
        AnalyzeImage(new Uri(ImageUrl.Text));
    }

    private void AnalyzeImage(Uri imageUri)
    {
        var url = Environment.GetEnvironmentVariable("AZURE_VISION_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("AZURE_VISION_KEY");
        var serviceOptions = new VisionServiceOptions(url, new AzureKeyCredential(key));

        using var imageSource = VisionSource.FromUrl(imageUri);

        var analysisOptions = new ImageAnalysisOptions()
        {
            Features = ImageAnalysisFeature.Caption |
                ImageAnalysisFeature.Text |
                ImageAnalysisFeature.Objects
        };

        using var analyzer = new ImageAnalyzer(serviceOptions, imageSource, analysisOptions);

        ImageAnalysisResult analysisResult = analyzer.Analyze();

        if (analysisResult.Reason == ImageAnalysisResultReason.Analyzed)
        {

            if (analysisResult.Caption != null)
            {
                ContentText.Text = $"{analysisResult.Caption.Content} (Confidence: {analysisResult.Caption.Confidence:0.0000})";
            }

            if (analysisResult.Text != null)
            {
                foreach (var line in analysisResult.Text.Lines)
                {
                    foreach (var word in line.Words)
                    {
                        DrawPolygon(word.BoundingPolygon, Colors.Yellow, word.Content);
                    }
                }
            }

            if (analysisResult.Objects != null)
            {
                foreach (var item in analysisResult.Objects)
                {
                    DrawPolygon(RectangleToPoints(item.BoundingBox), Colors.GreenYellow, item.Name);
                }
            }

        }
        else
        {
            var errorDetails = ImageAnalysisErrorDetails.FromResult(analysisResult);
            var sb = new StringBuilder();

            sb.AppendLine($"Error reason: {errorDetails.Reason}");
            sb.AppendLine($"Error code: {errorDetails.ErrorCode}");
            sb.AppendLine($"Error message: {errorDetails.Message}");

            MessageBox.Show(sb.ToString());
        }
    }

    private void DrawPolygon(IReadOnlyList<System.Drawing.Point> points, Color color, string? text = null)
    {
        Polygon polygon = new()
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2
        };

        if (text is not null)
        {
            polygon.ToolTip = new TextBlock() { Text = text };
        }

        foreach (var point in points)
        {
            polygon.Points.Add(new Point(point.X, point.Y));
        }

        MyCanvas.Children.Add(polygon);
    }

    private IReadOnlyList<System.Drawing.Point> RectangleToPoints(System.Drawing.Rectangle rect)
    {
        return new List<System.Drawing.Point>
    {
        new System.Drawing.Point(rect.Left, rect.Top),
        new System.Drawing.Point(rect.Right, rect.Top),
        new System.Drawing.Point(rect.Right, rect.Bottom),
        new System.Drawing.Point(rect.Left, rect.Bottom)
    }.AsReadOnly();
    }




    private void LoadImageFromUrl(Uri imageUri)
    {
        try
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = imageUri;
            bitmap.EndInit();

            TheImage.Source = bitmap;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading image: " + ex.Message);
        }
    }

}