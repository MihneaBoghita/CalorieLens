using SQLite;

namespace CalorieLens.Models
{
    public class FoodEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int UserId { get; set; }
        public string FoodName { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fat { get; set; }
        public DateTime Date { get; set; }
    }
}