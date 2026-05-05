using SQLite;
using CalorieLens.Models;

namespace CalorieLens.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _database;

    public DatabaseService()
    {
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"calorielens.db");

        _database = new SQLiteAsyncConnection(dbPath);

        _database.CreateTableAsync<User>().Wait();
    }

    public async Task AddUser(User user)
    {
        await _database.InsertAsync(user);
    }

    public async Task<User> GetUser(string username, string password)
    {
        return await _database.Table<User>()
            .Where(u =>
                u.Username == username &&
                u.Password == password)
            .FirstOrDefaultAsync();
    }

    public async Task<User> GetUserByUsername(string username)
    {
        return await _database.Table<User>()
            .FirstOrDefaultAsync(u => u.Username == username);
    }
}