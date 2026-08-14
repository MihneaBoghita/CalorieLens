namespace CalorieLens.Models
{
    /// <summary>
    /// Intrare alimentara — stocata in Firestore sub foodEntries/{autoId}
    /// </summary>
    public class FoodEntry
    {
        // Id-ul documentului Firestore (inlocuieste int Id din SQLite)
        public string FirebaseId { get; set; } = string.Empty;

        // Pastrat pentru compatibilitate cu codul existent
        public int Id { get; set; } = 0;

        // UID Firebase al userului proprietar
        public string UserId_Firebase { get; set; } = string.Empty;

        // Pastrat pentru compatibilitate cu GetTodayEntries(int userId)
        public int UserId { get; set; } = 0;

        public string FoodName { get; set; } = string.Empty;
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fat { get; set; }
        public double Sugar { get; set; }
        public DateTime Date { get; set; }
    }
}
