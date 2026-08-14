namespace CalorieLens.Models
{
    /// <summary>
    /// Inregistrare zilnica a greutatii — stocata in Firestore sub weightEntries/{autoId}
    /// </summary>
    public class WeightEntry
    {
        public string FirebaseId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public double Weight { get; set; }
        public DateTime Date { get; set; }
    }
}