using SQLite;

namespace CalorieLens.Models
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }

        public double Weight { get; set; }
        public double Height { get; set; }
        public double TargetWeight { get; set; }

        // Campuri noi pentru calculul caloric
        public int Age { get; set; }
        public string Sex { get; set; } // "Male" / "Female"
        public string ActivityLevel { get; set; } // "Sedentary","Light","Moderate","Active","VeryActive"
    }
}