using CalorieLens.Helpers;
using CalorieLens.Services;
using CalorieLens.Models;

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
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var user = new User
        {
            Username = usernameEntry.Text,
            Password = passwordEntry.Text
        };

        await _db.AddUser(user);

        await Navigation.PushAsync(new MainPage());
    }
}