using SQLite;
using CalorieLens.Models;

namespace CalorieLens.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _database;

    public DatabaseService()
    {
        // Fix: cale corecta pentru Android/iOS
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "calorielens.db");

        _database = new SQLiteAsyncConnection(dbPath);
        _database.CreateTableAsync<User>().Wait();
        _database.CreateTableAsync<FoodEntry>().Wait();
    }

    // ── User ──────────────────────────────────────
    public async Task AddUser(User user)
        => await _database.InsertAsync(user);

    public async Task UpdateUser(User user)
        => await _database.UpdateAsync(user);

    public async Task<User> GetUser(string username, string password)
        => await _database.Table<User>()
            .Where(u => u.Username == username && u.Password == password)
            .FirstOrDefaultAsync();

    public async Task<User> GetUserByUsername(string username)
        => await _database.Table<User>()
            .FirstOrDefaultAsync(u => u.Username == username);

    // ── FoodEntry ─────────────────────────────────
    public async Task AddFoodEntry(FoodEntry entry)
        => await _database.InsertAsync(entry);

    public async Task<List<FoodEntry>> GetTodayEntries(int userId)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return await _database.Table<FoodEntry>()
            .Where(f => f.UserId == userId && f.Date >= today && f.Date < tomorrow)
            .ToListAsync();
    }

    public async Task DeleteFoodEntry(int id)
        => await _database.DeleteAsync<FoodEntry>(id);
}