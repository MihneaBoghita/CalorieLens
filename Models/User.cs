namespace CalorieLens.Models
{
    /// <summary>
    /// Model utilizator — fara SQLite, stocat in Firestore sub users/{username}
    /// </summary>
    public class User
    {
        // Id-ul generat de Firebase Auth (inlocuieste int Id din SQLite)
        public string FirebaseUid { get; set; } = string.Empty;

        // Pastrat pentru compatibilitate cu codul existent (MainPage, etc.)
        // Nu mai e stocat in DB — e intotdeauna 0
        public int Id { get; set; } = 0;

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public double Weight { get; set; }
        public double Height { get; set; }
        public double TargetWeight { get; set; }

        public int Age { get; set; }
        public string Sex { get; set; } = string.Empty; // "Male" / "Female"
        public string ActivityLevel { get; set; } = string.Empty; // "Sedentary","Light","Moderate","Active","Very Active"

        // Data tinta pentru atingerea greutatii dorite (null = nesetat)
        public DateTime? GoalDate { get; set; }

        // True daca userul a atins data tinta — trece automat pe mentinere
        public bool IsMaintenanceMode { get; set; } = false;
    }
}