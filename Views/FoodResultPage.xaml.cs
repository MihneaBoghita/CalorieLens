using CalorieLens.Models;
using CalorieLens.Services;
using System.Text.RegularExpressions;

namespace CalorieLens.Views;

public partial class FoodResultPage : ContentPage
{
    private readonly string? _imagePath;
    private readonly string _extraDetails;
    private readonly string? _manualDescription;
    private string _analysisResult = string.Empty;
    private FoodEntry? _parsedEntry;
    private bool _saved = false;

    /// <summary>
    /// <param name="imagePath">Calea locala catre imaginea selectata.</param>
    /// <param name="extraDetails">Detalii suplimentare de la utilizator (optional).</param>
    /// </summary>
    public FoodResultPage(string imagePath, string extraDetails = "")
    {
        InitializeComponent();
        _imagePath = imagePath;
        _extraDetails = extraDetails;
        _manualDescription = null;
    }

    private FoodResultPage(string manualDescription, bool isManual)
    {
        InitializeComponent();
        _imagePath = null;
        _extraDetails = string.Empty;
        _manualDescription = manualDescription;
    }

    /// <summary>
    /// Creeaza o pagina de rezultat pentru introducere manuala (fara poza) —
    /// descrierea userului e trimisa direct la Gemini.
    /// </summary>
    public static FoodResultPage ForManualEntry(string description)
        => new FoodResultPage(description, true);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
            foodImage.Source = ImageSource.FromFile(_imagePath);
        else
        {
            imageFrame.IsVisible = false;
            retryButton.Text = "🔄 Încearcă din nou";
        }
        await AnalyzeFoodAsync();
    }

    private async Task AnalyzeFoodAsync()
    {
        loadingLayout.IsVisible = true;
        resultCard.IsVisible = false;
        errorCard.IsVisible = false;
        saveButton.IsVisible = false;
        _saved = false;

        try
        {
            // Trimite fie imaginea + detalii, fie doar descrierea text (introducere manuala)
            _analysisResult = _manualDescription != null
                ? await FoodAIService.AnalyzeFoodText(_manualDescription)
                : await FoodAIService.AnalyzeFood(_imagePath!, _extraDetails);

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

    private FoodEntry? ParseResult(string text)
    {
        try
        {
            var entry = new FoodEntry
            {
                UserId = App.CurrentUser?.Id ?? 0,
                UserId_Firebase = App.CurrentUser?.FirebaseUid ?? string.Empty,
                Date = DateTime.Now
            };

            entry.FoodName = ExtractValue(text, @"Mancare:\s*(.+)") ?? "Mancare necunoscuta";
            entry.Calories = ExtractDouble(text, @"Calorii estimate:\s*[\d.,]+");
            entry.Calories = ExtractDouble(text, @"Calorii estimate:\s*([\d.,]+)");
            entry.Protein = ExtractDouble(text, @"Proteine:\s*~?([\d.,]+)");
            entry.Carbs = ExtractDouble(text, @"Carbohidrati:\s*~?([\d.,]+)");
            entry.Sugar = ExtractDouble(text, @"Zahar:\s*~?([\d.,]+)");
            entry.Fat = ExtractDouble(text, @"Grasimi:\s*~?([\d.,]+)");

            return entry.Calories > 0 ? entry : null;
        }
        catch { return null; }
    }

    private string? ExtractValue(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private double ExtractDouble(string text, string pattern)
    {
        var val = ExtractValue(text, pattern);
        if (val == null) return 0;
        val = val.Replace(",", ".");
        return double.TryParse(val, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private async void OnSaveResult(object sender, EventArgs e)
    {
        if (_parsedEntry == null || _saved) return;
        _saved = true;
        saveButton.IsEnabled = false;
        saveButton.Text = "⏳ Se salveaza...";
        try
        {
            await App.Database.AddFoodEntry(_parsedEntry);
            saveButton.Text = "✅ Salvat";
            await DisplayAlert("✅ Salvat", $"{_parsedEntry.FoodName} a fost adaugat in jurnal!", "OK");

            // ← ADAUGĂ ACEASTĂ LINIE înainte de Pop
            MessagingCenter.Send<FoodResultPage>(this, "FoodSaved");

            await Navigation.PopAsync(); // iesi din FoodResultPage
            await Navigation.PopAsync(); // iesi din CameraPage
        }
        catch (Exception ex)
        {
            _saved = false;
            saveButton.IsEnabled = true;
            saveButton.Text = "💾 Salveaza";
            await DisplayAlert("Eroare", $"Nu s-a putut salva: {ex.Message}", "OK");
        }
    }

    private async void OnScanAgain(object sender, EventArgs e)
        => await Navigation.PopAsync();
}