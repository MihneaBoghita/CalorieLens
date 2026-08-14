using CalorieLens.Helpers;
using CalorieLens.Services;
using CalorieLens.Views;

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
            // Seteaza userul curent in ambele locuri
            Session.CurrentUser = user;
            App.CurrentUser = user;

            // Navigheaza spre pagina cu swipe (Main + Jurnal)
            await Navigation.PushAsync(new MainCarouselPage(user));
        }
        else
        {
            await DisplayAlert("Error", "Username sau parolă incorecte.", "OK");
        }
    }

    private async void OnShowPasswordClicked(object sender, EventArgs e)
    {
        passwordEntry.IsPassword = !passwordEntry.IsPassword;
    }

    private async void GoToRegister(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_db));
    }
}