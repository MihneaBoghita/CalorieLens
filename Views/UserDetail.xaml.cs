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
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        _currentUser.Height = Convert.ToDouble(heightEntry.Text);
        _currentUser.Weight = Convert.ToDouble(weightEntry.Text);
        _currentUser.TargetWeight = Convert.ToDouble(weightGoalEntry.Text);

        await _db.UpdateUser(_currentUser);

        await Navigation.PushAsync(new MainPage());
    }
}