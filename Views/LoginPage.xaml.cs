using CalorieLens.Helpers;
using CalorieLens.Services;

namespace CalorieLens;

public partial class LoginPage : ContentPage
{
    private readonly DatabaseService _db;

    public LoginPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var user = await _db.GetUser(usernameEntry.Text, passwordEntry.Text);

        if (user != null)
        {
            Session.CurrentUser = user;
            await Navigation.PushAsync(new MainPage());
        }
        else
        {
            await DisplayAlert("Error", "Invalid login", "OK");
        }
    }

    private async void GoToRegister(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_db));
    }
}