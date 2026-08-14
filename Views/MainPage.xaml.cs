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

            MessagingCenter.Subscribe<FoodResultPage>(this, "FoodSaved", async (_) =>
            {
                await MainThread.InvokeOnMainThreadAsync(RefreshData);
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 1. Verifica daca data goal a trecut -> trece automat pe mentinere
            await CheckAndUpdateMaintenanceMode();

            // 2. Intreaba greutatea zilnica (o singura data pe zi)
            await AskDailyWeight();

            // 3. Refresh date principale
            await RefreshData();
        }

        /// <summary>
        /// Daca GoalDate a trecut si userul nu e deja pe mentinere,
        /// actualizeaza automat ActivityLevel pe mentinere si salveaza in DB.
        /// </summary>
        private async Task CheckAndUpdateMaintenanceMode()
        {
            if (_user.IsMaintenanceMode) return;
            if (!_user.GoalDate.HasValue) return;
            if (DateTime.Today < _user.GoalDate.Value.Date) return;

            // Data a trecut — trecem pe mentinere
            _user.TargetWeight = _user.Weight; // goal = greutate curenta
            _user.IsMaintenanceMode = true;
            await _db.UpdateUser(_user);
            App.CurrentUser = _user;

            await DisplayAlert(
                "🎉 Felicitări!",
                "Ai atins data țintă! Aplicația te-a trecut automat pe modul de menținere. " +
                "Continuă să îți monitorizezi alimentația pentru a-ți păstra greutatea.",
                "OK");

            // Recalculeaza caloriile zilnice pe mentinere
            _dailyCalorieGoal = CalculateDailyCalories(_user);
        }

        /// <summary>
        /// Afiseaza un prompt o singura data pe zi pentru a inregistra greutatea curenta.
        /// Foloseste WeightEntry din Firestore — daca exista una pentru azi, nu mai intreaba.
        /// </summary>
        private async Task AskDailyWeight()
        {
            var existing = await _db.GetTodayWeightEntry();
            if (existing != null) return; // deja inregistrata azi

            var result = await DisplayPromptAsync(
                "⚖️ Greutatea de azi",
                "Cât cântărești astăzi? (kg)",
                accept: "Salvează",
                cancel: "Sari peste",
                placeholder: $"{_user.Weight:F1}",
                keyboard: Keyboard.Numeric);

            if (result == null) return; // a apasut Sari peste

            if (!double.TryParse(result, out var weight) || weight < 20 || weight > 400)
            {
                await DisplayAlert("Valoare invalidă", "Introdu o greutate validă între 20 și 400 kg.", "OK");
                return;
            }

            // Salveaza inregistrarea de greutate
            var entry = new WeightEntry
            {
                UserId = _user.FirebaseUid,
                Weight = weight,
                Date = DateTime.Now
            };
            await _db.AddWeightEntry(entry);

            // Actualizeaza greutatea curenta a userului
            _user.Weight = weight;
            await _db.UpdateUser(_user);
            App.CurrentUser = _user;

            // Recalculeaza caloriile cu noua greutate
            _dailyCalorieGoal = CalculateDailyCalories(_user);
        }

        private async Task RefreshData()
        {
            var entries = await _db.GetTodayEntries();

            double totalCal = entries.Sum(e => e.Calories);
            double totalProtein = entries.Sum(e => e.Protein);
            double totalCarbs = entries.Sum(e => e.Carbs);
            double totalFat = entries.Sum(e => e.Fat);
            double totalSugar = entries.Sum(e => e.Sugar);

            // Header
            greetingLabel.Text = $"Buna, {_user.Username}! 👋";

            if (_user.IsMaintenanceMode)
            {
                goalLabel.Text = "⚖️ Menținere";
            }
            else
            {
                var diff = _user.TargetWeight - _user.Weight;
                goalLabel.Text = diff < 0 ? "🔥 Deficit caloric activ"
                               : diff > 0 ? "💪 Surplus caloric activ"
                               : "⚖️ Menținere";
            }

            // Centrul roții — calorii clare
            caloriesEatenLabel.Text = $"{(int)totalCal}";
            var remaining = _dailyCalorieGoal - totalCal;
            caloriesRemainingLabel.Text = remaining >= 0
                ? $"{(int)remaining} rămase"
                : $"{(int)Math.Abs(remaining)} peste limită";

            // Praguri zilnice recomandate
            double proteinGoal = _dailyCalorieGoal * 0.30 / 4;
            double carbsGoal = _dailyCalorieGoal * 0.45 / 4;
            double fatGoal = _dailyCalorieGoal * 0.25 / 9;
            double sugarGoal = 50.0;

            // Legenda — text complet
            proteinLabel.Text = $"Proteine {(int)totalProtein}g / {(int)proteinGoal}g";
            carbsLabel.Text = $"Carbohidrați {(int)totalCarbs}g / {(int)carbsGoal}g";
            fatLabel.Text = $"Grăsimi {(int)totalFat}g / {(int)fatGoal}g";
            sugarLabel.Text = $"Zahăr {(int)totalSugar}g / 50g";

            _drawable.CalorieProgress = (float)Math.Min(totalCal / _dailyCalorieGoal, 1.0);
            _drawable.ProteinProgress = (float)Math.Min(totalProtein / proteinGoal, 1.0);
            _drawable.CarbsProgress = (float)Math.Min(totalCarbs / carbsGoal, 1.0);
            _drawable.FatProgress = (float)Math.Min(totalFat / fatGoal, 1.0);
            _drawable.SugarProgress = (float)Math.Min(totalSugar / sugarGoal, 1.0);
            macroRingsView.Invalidate();

            // Lista — macro summary text complet
            var displayList = entries.Select(e => new FoodDisplayItem
            {
                FoodName = e.FoodName,
                CaloriesDisplay = $"{(int)e.Calories} kcal",
                MacroSummary = $"Proteine:{(int)e.Protein}g  Carb:{(int)e.Carbs}g  Grăsimi:{(int)e.Fat}g  Zahăr:{(int)e.Sugar}g"
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
                _midnightTimer.Interval = 86400000;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // La miezul noptii verifica si maintenance mode si reincarca datele
                    await CheckAndUpdateMaintenanceMode();
                    await RefreshData();
                });
            };
            _midnightTimer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _midnightTimer?.Stop();
            MessagingCenter.Unsubscribe<FoodResultPage>(this, "FoodSaved");
        }

        private async void OnOpenCamera(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CameraPage());
        }

        private async void OnOpenManualEntry(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ManualFoodEntryPage());
        }

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

            // Pe mentinere
            if (u.IsMaintenanceMode) return tdee;

            double weightDiff = u.TargetWeight - u.Weight; // negativ = slabit, pozitiv = luat in greutate

            // Daca e deja la goal
            if (Math.Abs(weightDiff) < 0.5) return tdee;

            // Daca are GoalDate setata, calculeaza deficitul/surplusul necesar
            if (u.GoalDate.HasValue && u.GoalDate.Value.Date > DateTime.Today)
            {
                int daysLeft = (u.GoalDate.Value.Date - DateTime.Today).Days;

                // 1 kg de grasime = ~7700 kcal
                double totalKcalNeeded = weightDiff * 7700;
                double dailyAdjustment = totalKcalNeeded / daysLeft;

                // Limiteaza la -1000/+1000 kcal pe zi (sigur din punct de vedere medical)
                dailyAdjustment = Math.Max(-1000, Math.Min(1000, dailyAdjustment));

                return tdee + dailyAdjustment;
            }

            // Fallback daca nu are GoalDate — deficit/surplus fix de 500 kcal
            if (weightDiff < -0.5) return tdee - 500;
            if (weightDiff > 0.5) return tdee + 500;
            return tdee;
        }
    }

    public class FoodDisplayItem
    {
        public string FoodName { get; set; }
        public string CaloriesDisplay { get; set; }
        public string MacroSummary { get; set; }
    }

    public class MacroRingsDrawable : IDrawable
    {
        public float CalorieProgress { get; set; }
        public float ProteinProgress { get; set; }
        public float CarbsProgress { get; set; }
        public float FatProgress { get; set; }
        public float SugarProgress { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float cx = dirtyRect.Width / 2;
            float cy = dirtyRect.Height / 2;

            DrawRing(canvas, cx, cy, 130, 14, CalorieProgress, "#22C55E", "#1A2E1A");
            DrawRing(canvas, cx, cy, 110, 12, ProteinProgress, "#3B82F6", "#0F1E2E");
            DrawRing(canvas, cx, cy, 92, 12, CarbsProgress, "#F59E0B", "#2E2510");
            DrawRing(canvas, cx, cy, 74, 12, FatProgress, "#EF4444", "#2E1010");
            DrawRing(canvas, cx, cy, 56, 12, SugarProgress, "#A855F7", "#1E0A2E");
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

            // Fundal — cerc complet, sens orar
            canvas.StrokeColor = Color.FromArgb(bgColor);
            canvas.DrawArc(x, y, size, size, 90, -360, false, false);

            // Progres — sens orar, porneste din sus
            if (progress > 0)
            {
                canvas.StrokeColor = Color.FromArgb(color);
                canvas.DrawArc(x, y, size, size, 90, -(progress * 360), false, false);
            }
        }
    }
}