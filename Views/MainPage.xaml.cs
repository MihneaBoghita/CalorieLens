using CalorieLens.Models;
using CalorieLens.Services;
using CalorieLens.Views;
using Microsoft.Maui.Graphics;

namespace CalorieLens
{
    public partial class MainPage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _db;
        private double _dailyCalorieGoal;
        private MacroRingsDrawable _drawable;
        private System.Timers.Timer _midnightTimer;

        public MainPage(User user)
        {
            InitializeComponent();
            _user = user;
            _db = App.Database;
            _dailyCalorieGoal = CalculateDailyCalories(user);

            _drawable = new MacroRingsDrawable();
            macroRingsView.Drawable = _drawable;

            SetupMidnightReset();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RefreshData();
        }

        private async Task RefreshData()
        {
            var entries = await _db.GetTodayEntries(_user.Id);

            double totalCal = entries.Sum(e => e.Calories);
            double totalProtein = entries.Sum(e => e.Protein);
            double totalCarbs = entries.Sum(e => e.Carbs);
            double totalFat = entries.Sum(e => e.Fat);

            // Header
            greetingLabel.Text = $"Buna, {_user.Username}! 👋";
            var diff = _user.TargetWeight - _user.Weight;
            goalLabel.Text = diff < 0 ? "🔥 Deficit caloric activ"
                           : diff > 0 ? "💪 Surplus caloric activ"
                           : "⚖️ Mentinere";

            // Centrul rotii
            caloriesEatenLabel.Text = $"{(int)totalCal}";
            var remaining = _dailyCalorieGoal - totalCal;
            caloriesRemainingLabel.Text = remaining >= 0
                ? $"{(int)remaining} ramase"
                : $"{(int)Math.Abs(remaining)} peste";

            // Legenda
            proteinLabel.Text = $"P {(int)totalProtein}g";
            carbsLabel.Text = $"C {(int)totalCarbs}g";
            fatLabel.Text = $"G {(int)totalFat}g";

            // Deseneaza rotile
            _drawable.CalorieProgress = (float)Math.Min(totalCal / _dailyCalorieGoal, 1.0);
            _drawable.ProteinProgress = (float)Math.Min(totalProtein / (_dailyCalorieGoal * 0.3 / 4), 1.0);
            _drawable.CarbsProgress = (float)Math.Min(totalCarbs / (_dailyCalorieGoal * 0.45 / 4), 1.0);
            _drawable.FatProgress = (float)Math.Min(totalFat / (_dailyCalorieGoal * 0.25 / 9), 1.0);
            macroRingsView.Invalidate();

            // Lista
            var displayList = entries.Select(e => new FoodDisplayItem
            {
                FoodName = e.FoodName,
                CaloriesDisplay = $"{(int)e.Calories} kcal",
                MacroSummary = $"P:{(int)e.Protein}g  C:{(int)e.Carbs}g  G:{(int)e.Fat}g"
            }).ToList();

            foodList.ItemsSource = displayList;
            emptyLabel.IsVisible = displayList.Count == 0;
        }

        private void SetupMidnightReset()
        {
            var now = DateTime.Now;
            var midnight = now.Date.AddDays(1);
            var msUntilMidnight = (midnight - now).TotalMilliseconds;

            _midnightTimer = new System.Timers.Timer(msUntilMidnight);
            _midnightTimer.Elapsed += async (s, e) =>
            {
                _midnightTimer.Interval = 86400000; // 24h dupa primul reset
                await MainThread.InvokeOnMainThreadAsync(RefreshData);
            };
            _midnightTimer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _midnightTimer?.Stop();
        }

        private async void OnOpenCamera(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CameraPage());
        }

        // ── Harris-Benedict + multiplicator activitate ─────────────
        private double CalculateDailyCalories(User u)
        {
            double bmr = u.Sex == "Male"
                ? 88.362 + (13.397 * u.Weight) + (4.799 * u.Height) - (5.677 * u.Age)
                : 447.593 + (9.247 * u.Weight) + (3.098 * u.Height) - (4.330 * u.Age);

            double multiplier = u.ActivityLevel switch
            {
                "Sedentary" => 1.2,
                "Light" => 1.375,
                "Moderate" => 1.55,
                "Active" => 1.725,
                "Very Active" => 1.9,
                _ => 1.2
            };

            double tdee = bmr * multiplier;

            double diff = u.TargetWeight - u.Weight;
            if (diff < -2) return tdee - 500; // deficit
            else if (diff > 2) return tdee + 500; // surplus
            else return tdee;          // mentinere
        }
    }

    // ── Display model pentru lista ─────────────────────────────
    public class FoodDisplayItem
    {
        public string FoodName { get; set; }
        public string CaloriesDisplay { get; set; }
        public string MacroSummary { get; set; }
    }

    // ── Desenarea rotilor cu Graphics ──────────────────────────
    public class MacroRingsDrawable : IDrawable
    {
        public float CalorieProgress { get; set; }
        public float ProteinProgress { get; set; }
        public float CarbsProgress { get; set; }
        public float FatProgress { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float cx = dirtyRect.Width / 2;
            float cy = dirtyRect.Height / 2;

            DrawRing(canvas, cx, cy, 110, 18, CalorieProgress, "#22C55E", "#1A2E1A");
            DrawRing(canvas, cx, cy, 86, 14, ProteinProgress, "#22C55E", "#1A2E1A");
            DrawRing(canvas, cx, cy, 66, 14, CarbsProgress, "#F59E0B", "#2E2510");
            DrawRing(canvas, cx, cy, 46, 14, FatProgress, "#EF4444", "#2E1010");
        }

        private void DrawRing(ICanvas canvas, float cx, float cy,
                               float radius, float thickness,
                               float progress, string color, string bgColor)
        {
            float x = cx - radius;
            float y = cy - radius;
            float size = radius * 2;

            canvas.StrokeSize = thickness;
            canvas.StrokeLineCap = LineCap.Round;

            // Fundal inel
            canvas.StrokeColor = Color.FromArgb(bgColor);
            canvas.DrawArc(x, y, size, size, 90, -360, false, false);

            if (progress > 0)
            {
                canvas.StrokeColor = Color.FromArgb(color);
                canvas.DrawArc(x, y, size, size, 90, -(progress * 360), false, false);
            }
        }
    }
}
    