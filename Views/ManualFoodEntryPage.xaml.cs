namespace CalorieLens.Views;

public partial class ManualFoodEntryPage : ContentPage
{
    public ManualFoodEntryPage()
    {
        InitializeComponent();
    }

    private async void OnAnalyze(object sender, EventArgs e)
    {
        var description = descriptionEditor.Text?.Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            await DisplayAlert("Eroare", "Scrie ce ai mâncat înainte de a analiza.", "OK");
            return;
        }

        await Navigation.PushAsync(FoodResultPage.ForManualEntry(description));
    }
}