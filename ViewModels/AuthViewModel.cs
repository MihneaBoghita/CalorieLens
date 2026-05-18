using CalorieLens.Models;
using CalorieLens.Services;
using CalorieLens.Views;
using System.Windows.Input;

namespace CalorieLens.ViewModels
{
    internal class AuthViewModel
    {
        private readonly DatabaseService _db;
        private readonly Page _page;

        public string Username { get; set; }
        public string Password { get; set; }

        public ICommand RegisterCommand { get; }
        public ICommand LoginCommand { get; }

        public AuthViewModel(DatabaseService db, Page page)
        {
            _db = db;
            _page = page;

            RegisterCommand = new Command(async () => await Register());
            LoginCommand = new Command(async () => await Login());
        }

        private async Task Register()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await _page.DisplayAlert("Eroare", "Completeaza username si parola.", "OK");
                return;
            }

            var existing = await _db.GetUserByUsername(Username);
            if (existing != null)
            {
                await _page.DisplayAlert("Eroare", "Username-ul este deja folosit.", "OK");
                return;
            }

            var user = new User { Username = Username, Password = Password };
            await _db.AddUser(user);

            // Dupa register -> completeaza profilul
            var newUser = await _db.GetUserByUsername(Username);
            App.CurrentUser = newUser;
            await _page.Navigation.PushAsync(new UserDetail(_db, newUser));
        }

        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await _page.DisplayAlert("Eroare", "Completeaza username si parola.", "OK");
                return;
            }

            var user = await _db.GetUser(Username, Password);
            if (user == null)
            {
                await _page.DisplayAlert("Eroare", "Username sau parola incorecta.", "OK");
                return;
            }

            App.CurrentUser = user;

            // Daca profilul nu e completat -> UserDetail, altfel -> MainPage
            if (user.Age == 0 || string.IsNullOrEmpty(user.ActivityLevel))
                await _page.Navigation.PushAsync(new UserDetail(_db, user));
            else
                await _page.Navigation.PushAsync(new MainPage(user));
        }
    }
}