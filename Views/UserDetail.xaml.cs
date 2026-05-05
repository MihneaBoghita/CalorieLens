namespace CalorieLens.Views;

public partial class UserDetail : ContentPage
{
    private string selectedGoal = "Maintain";

    public UserDetail()
    {
        InitializeComponent();
    }

    private void OnGoalSelected(object sender, EventArgs e)
    {
        var btn = sender as Button;

        // reset colors
        loseBtn.BackgroundColor = Color.FromArgb("#1F2937");
        maintainBtn.BackgroundColor = Color.FromArgb("#1F2937");
        gainBtn.BackgroundColor = Color.FromArgb("#1F2937");

        // highlight selected
        btn.BackgroundColor = Color.FromArgb("#22C55E");

        selectedGoal = btn.Text;
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Goal", selectedGoal, "OK");
    }
}