using CalorieLens.Services;

namespace CalorieLens.Views;

public partial class FoodResultPage : ContentPage
{
    private readonly string _imagePath;
    private string _analysisResult = string.Empty;

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

            if (_analysisResult.StartsWith("Eroare"))
            {
                errorLabel.Text = _analysisResult;
                errorCard.IsVisible = true;
            }
            else
            {
                resultLabel.Text = _analysisResult;
                resultCard.IsVisible = true;
                saveButton.IsVisible = true;
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

    private async void OnScanAgain(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnSaveResult(object sender, EventArgs e)
    {
        // Extinde aici pentru a salva in DB
        await DisplayAlert("Salvat", "Rezultatul a fost salvat!", "OK");
    }
}