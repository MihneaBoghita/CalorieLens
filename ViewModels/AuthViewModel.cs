using CalorieLens.Helpers;
using CalorieLens.Models;
using CalorieLens.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace CalorieLens.ViewModels
{
    internal class AuthViewModel
    {
        private readonly DatabaseService _db;

        public string Username { get; set; }
        public string Password { get; set; }

        public ICommand RegisterCommand { get; }
        public ICommand LoginCommand { get; }

        public AuthViewModel(DatabaseService db)
        {
            _db = db;

            RegisterCommand = new Command(async () => await Register());
            LoginCommand = new Command(async () => await Login());
        }

        private async Task Register()
        {
            var existing = await _db.GetUserByUsername(Username);

            if (existing != null)
                return;

            var user = new User
            {
                Username = Username,
                Password = Password,
                DailyCaloriesGoal = 2000
            };

            await _db.AddUser(user);
        }

        private async Task Login()
        {
            var user = await _db.GetUser(Username, Password);

            if (user != null)
            {
                Session.CurrentUser = user;
            }
        }
    }
}
