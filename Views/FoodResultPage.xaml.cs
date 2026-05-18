using CalorieLens.Models;
using CalorieLens.Services;
using System.Text.RegularExpressions;

namespace CalorieLens.Views;

public partial class FoodResultPage : ContentPage
{
    private readonly string _imagePath;
    private string _analysisResult = string.Empty;
    private FoodEntry? _parsedEntry;

    public FoodResultPage(string imagePath)
    {
        InitializeComponent();
        _imagePath = imagePath;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (File.Exists(_imagePath))
            foodImage.Source = ImageSource.FromFile(_imagePath);
        await AnalyzeFoodAsync();
    }

    private async Task AnalyzeFoodAsync()
    {
        loadingLayout.IsVisible = true;
        resultCard.IsVisible = false;
        errorCard.IsVisible = false;
        saveButton.IsVisible = false;

        try
        {
            _analysisResult = await FoodAIService.AnalyzeFood(_imagePath);

            if (_analysisResult.StartsWith("Eroare") || _analysisResult.StartsWith("Server"))
            {
                errorLabel.Text = _analysisResult;
                errorCard.IsVisible = true;
            }
            else
            {
                resultLabel.Text = _analysisResult;
                resultCard.IsVisible = true;
                _parsedEntry = ParseResult(_analysisResult);
                saveButton.IsVisible = _parsedEntry != null;
            }
        }
        catch (Exception ex)
        {
            errorLabel.Text = $"Eroare neasteptata: {ex.Message}";
            errorCard.IsVisible = true;
        }
        finally
        {
            loadingLayout.IsVisible = false;
        }
    }

    // Parseaza raspunsul Gemini si extrage valorile numerice
    private FoodEntry? ParseResult(string text)
    {
        try
        {
            var entry = new FoodEntry
            {
                UserId = App.CurrentUser?.Id ?? 0,
                Date = DateTime.Now
            };

            entry.FoodName = ExtractValue(text, @"Mancare:\s*(.+)") ?? "Mancare necunoscuta";
            entry.Calories = ExtractDouble(text, @"Calorii estimate:\s*([\d.]+)");
            entry.Protein = ExtractDouble(text, @"Proteine:\s*~?([\d.]+)");
            entry.Carbs = ExtractDouble(text, @"Carbohidrati:\s*~?([\d.]+)");
            entry.Fat = ExtractDouble(text, @"Grasimi:\s*~?([\d.]+)");

            return entry.Calories > 0 ? entry : null;
        }
        catch { return null; }
    }

    private string? ExtractValue(string text, string pattern)
    {
        var match = Regex.Match(text, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private double ExtractDouble(string text, string pattern)
    {
        var val = ExtractValue(text, pattern);
        return double.TryParse(val, out var result) ? result : 0;
    }

    private async void OnSaveResult(object sender, EventArgs e)
    {
        if (_parsedEntry == null) return;

        await App.Database.AddFoodEntry(_parsedEntry);
        await DisplayAlert("✅ Salvat", $"{_parsedEntry.FoodName} a fost adaugat in jurnal!", "OK");
        saveButton.IsEnabled = false;
        saveButton.Text = "✅ Salvat";
    }

    private async void OnScanAgain(object sender, EventArgs e)
        => await Navigation.PopAsync();
}