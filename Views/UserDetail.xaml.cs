using CalorieLens.Models;
using CalorieLens.Services;

namespace CalorieLens.Views;

public partial class UserDetail : ContentPage
{
    private readonly DatabaseService _db;
    private User _currentUser;

    public UserDetail(DatabaseService db, User user)
    {
        InitializeComponent();
        _db = db;
        _currentUser = user;

        // Seteaza data minima la maine (nu poti pune goal in trecut)
        goalDatePicker.MinimumDate = DateTime.Today.AddDays(1);
        goalDatePicker.Date = DateTime.Today.AddMonths(3);
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(heightEntry.Text) ||
            string.IsNullOrWhiteSpace(weightEntry.Text) ||
            string.IsNullOrWhiteSpace(weightGoalEntry.Text) ||
            string.IsNullOrWhiteSpace(ageEntry.Text) ||
            sexPicker.SelectedIndex == -1 ||
            activityPicker.SelectedIndex == -1)
        {
            await DisplayAlert("Eroare", "Te rugam sa completezi toate campurile.", "OK");
            return;
        }

        if (!double.TryParse(heightEntry.Text, out var height) ||
            !double.TryParse(weightEntry.Text, out var weight) ||
            !double.TryParse(weightGoalEntry.Text, out var goal) ||
            !int.TryParse(ageEntry.Text, out var age))
        {
            await DisplayAlert("Eroare", "Valorile introduse nu sunt valide.", "OK");
            return;
        }

        _currentUser.Height = height;
        _currentUser.Weight = weight;
        _currentUser.TargetWeight = goal;
        _currentUser.Age = age;
        _currentUser.Sex = sexPicker.SelectedItem.ToString();
        _currentUser.ActivityLevel = activityPicker.SelectedItem.ToString();
        _currentUser.GoalDate = goalDatePicker.Date;
        _currentUser.IsMaintenanceMode = false;

        await _db.UpdateUser(_currentUser);
        App.CurrentUser = _currentUser;

        await Navigation.PushAsync(new MainPage(_currentUser));
    }
}