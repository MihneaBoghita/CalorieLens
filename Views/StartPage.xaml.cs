using CalorieLens.Services;

namespace CalorieLens;

public partial class StartPage : ContentPage
{
    private readonly DatabaseService _db;

    public StartPage(DatabaseService db)
    {
        InitializeComponent();

        _db = db;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage(_db));
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_db));
    }
}