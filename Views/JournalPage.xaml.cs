using CalorieLens.Models;
using CalorieLens.Services;

namespace CalorieLens.Views;

public partial class JournalPage : ContentPage
{
    private readonly DatabaseService _db;

    public JournalPage()
    {
        InitializeComponent();
        _db = App.Database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadWeightProgress();
        await LoadJournal();
    }

    // ── PROGRES GREUTATE ───────────────────────────────────────────────────

    private async Task LoadWeightProgress()
    {
        var weightEntries = await _db.GetAllWeightEntries();
        var user = App.CurrentUser;

        if (user == null) return;

        // Afiseaza rezumatul
        targetWeightLabel.Text = $"{user.TargetWeight:F1} kg";

        if (weightEntries.Count == 0)
        {
            startWeightLabel.Text = $"{user.Weight:F1} kg";
            currentWeightLabel.Text = $"{user.Weight:F1} kg";
            emptyWeightLabel.IsVisible = true;
            return;
        }

        emptyWeightLabel.IsVisible = false;

        var firstEntry = weightEntries.First();
        var lastEntry = weightEntries.Last();

        startWeightLabel.Text = $"{firstEntry.Weight:F1} kg";
        currentWeightLabel.Text = $"{lastEntry.Weight:F1} kg";

        // Construieste lista cu delta fata de ziua precedenta
        var displayList = new List<WeightDisplayItem>();
        for (int i = 0; i < weightEntries.Count; i++)
        {
            var entry = weightEntries[i];
            double? delta = i > 0 ? entry.Weight - weightEntries[i - 1].Weight : null;

            string deltaText = delta.HasValue
                ? (delta.Value >= 0 ? $"+{delta.Value:F1}" : $"{delta.Value:F1}")
                : "—";
            string deltaColor = !delta.HasValue ? "#6B7280"
                : delta.Value > 0 ? "#EF4444"
                : delta.Value < 0 ? "#22C55E"
                : "#6B7280";

            displayList.Add(new WeightDisplayItem
            {
                DateLabel = FormatDate(entry.Date.Date),
                WeightDisplay = $"{entry.Weight:F1} kg",
                DeltaDisplay = deltaText,
                DeltaColor = deltaColor
            });
        }

        // Cel mai recent primul
        displayList.Reverse();
        weightList.ItemsSource = displayList;
    }

    // ── JURNAL ALIMENTAR ───────────────────────────────────────────────────

    private async Task LoadJournal()
    {
        var entries = await _db.GetAllEntries();

        var grouped = entries
            .GroupBy(e => e.Date.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new JournalDayItem
            {
                DateLabel = FormatDate(g.Key),
                CaloriesDisplay = $"{(int)g.Sum(e => e.Calories)} kcal",
                ProteinDisplay = $"{(int)g.Sum(e => e.Protein)}g",
                CarbsDisplay = $"{(int)g.Sum(e => e.Carbs)}g",
                FatDisplay = $"{(int)g.Sum(e => e.Fat)}g",
                SugarDisplay = $"{(int)g.Sum(e => e.Sugar)}g"
            }).ToList();

        journalList.ItemsSource = grouped;
        emptyJournalLabel.IsVisible = grouped.Count == 0;
    }

    // ── HELPERS ────────────────────────────────────────────────────────────

    private static string FormatDate(DateTime date)
    {
        if (date == DateTime.Today) return "Azi";
        if (date == DateTime.Today.AddDays(-1)) return "Ieri";

        var zi = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Luni",
            DayOfWeek.Tuesday => "Marți",
            DayOfWeek.Wednesday => "Miercuri",
            DayOfWeek.Thursday => "Joi",
            DayOfWeek.Friday => "Vineri",
            DayOfWeek.Saturday => "Sâmbătă",
            DayOfWeek.Sunday => "Duminică",
            _ => ""
        };
        return $"{zi}, {date:dd MMM yyyy}";
    }
}

public class WeightDisplayItem
{
    public string DateLabel { get; set; }
    public string WeightDisplay { get; set; }
    public string DeltaDisplay { get; set; }
    public string DeltaColor { get; set; }
}

public class JournalDayItem
{
    public string DateLabel { get; set; }
    public string CaloriesDisplay { get; set; }
    public string ProteinDisplay { get; set; }
    public string CarbsDisplay { get; set; }
    public string FatDisplay { get; set; }
    public string SugarDisplay { get; set; }
}