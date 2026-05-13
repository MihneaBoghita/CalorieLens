using CalorieLens.Helpers;
using CalorieLens.Services;
using CalorieLens.Models;
using CalorieLens.Views;

namespace CalorieLens;

public partial class RegisterPage : ContentPage
{
    private readonly DatabaseService _db;
    public RegisterPage(DatabaseService db)
	{
		InitializeComponent();
        _db = db;
    }
    private async void GoToLogin(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage(_db));
    }
    private async void OnShowPasswordClicked(object sender, EventArgs e)
    {
        if(passwordEntry.IsPassword == true && passwordVerifcationEntry.IsPassword == true)
        {
            passwordEntry.IsPassword = false;
            passwordVerifcationEntry.IsPassword = false;
        }
        else
        {
            passwordEntry.IsPassword = true;
            passwordVerifcationEntry.IsPassword = true;
        }
    }
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var result = RegisterVerification.Verify(
            usernameEntry.Text,
            passwordEntry.Text,
            passwordVerifcationEntry.Text);

        if (!result.IsValid)
        {
            await DisplayAlert("Error", result.ErrorMessage, "OK");
            return;
        }
        var user = new User
        {
            Username = usernameEntry.Text,
            Password = passwordEntry.Text
        };

        await _db.AddUser(user);

        await Navigation.PushAsync(new UserDetail(_db, user));
    }
}