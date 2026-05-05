using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalorieLens.Services
{
    class RegisterVerification
    {
        public static (bool IsValid, string ErrorMessage) Verify(
        string username,
        string password,
        string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username cannot be empty");

            if (username.Length < 3)
                return (false, "Username must be at least 3 characters");

            if (password != confirmPassword)
                return (false, "Passwords do not match");

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password cannot be empty");

            if (password.Length < 6)
                return (false, "Password must be at least 6 characters");

            if (!username.Any(char.IsLetterOrDigit))
                return (false, "Username must contain only letters or digits");

            return (true, string.Empty);
        }
    }
}
