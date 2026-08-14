using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CalorieLens.Models;

namespace CalorieLens.Services;

/// <summary>
/// DatabaseService folosind Firebase Firestore REST API.
/// Nu necesita SDK nativ — functioneaza pe toate platformele MAUI.
///
/// Structura Firestore:
///   users/{username}           -> document User
///   foodEntries/{autoId}       -> document FoodEntry (cu camp userId)
///   weightEntries/{autoId}     -> document WeightEntry (cu camp userId + date + weight)
/// </summary>
public class DatabaseService
{
    // ── CONFIG ─────────────────────────────────────────────────────────────
    private const string ProjectId = "calorielens-dd3ac";
    private static readonly string ApiKey = Secrets.FirebaseApiKey;

    private const string FirestoreBase =
        $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents";

    private const string AuthBase =
        "https://identitytoolkit.googleapis.com/v1";

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private string? _idToken;
    private string? _localId;

    // ── AUTH ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Inregistreaza un user nou cu email+parola in Firebase Auth,
    /// apoi salveaza profilul extins in Firestore.
    /// </summary>
    public async Task AddUser(User user)
    {
        var signUpBody = JsonSerializer.Serialize(new
        {
            email = UsernameToEmail(user.Username),
            password = user.Password,
            returnSecureToken = true
        });

        var authResp = await PostAsync($"{AuthBase}/accounts:signUp?key={ApiKey}", signUpBody);
        var authDoc = JsonDocument.Parse(authResp);

        _idToken = authDoc.RootElement.GetProperty("idToken").GetString();
        _localId = authDoc.RootElement.GetProperty("localId").GetString();
        user.FirebaseUid = _localId!;

        await SetUserDocument(user);
    }

    /// <summary>
    /// Autentifica un user existent; returneaza null daca credentialele sunt gresite.
    /// </summary>
    public async Task<User?> GetUser(string username, string password)
    {
        try
        {
            var signInBody = JsonSerializer.Serialize(new
            {
                email = UsernameToEmail(username),
                password = password,
                returnSecureToken = true
            });

            var authResp = await PostAsync(
                $"{AuthBase}/accounts:signInWithPassword?key={ApiKey}", signInBody);
            var authDoc = JsonDocument.Parse(authResp);

            _idToken = authDoc.RootElement.GetProperty("idToken").GetString();
            _localId = authDoc.RootElement.GetProperty("localId").GetString();

            return await GetUserByUsername(username);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Verifica daca un username exista deja in Firestore.</summary>
    public async Task<User?> GetUserByUsername(string username)
    {
        try
        {
            var url = $"{FirestoreBase}/users/{Uri.EscapeDataString(username)}";
            var json = await GetAsync(url);
            return FirestoreDocToUser(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Actualizeaza profilul unui user in Firestore.</summary>
    public async Task UpdateUser(User user)
    {
        await SetUserDocument(user);
    }

    // ── FOOD ENTRIES ───────────────────────────────────────────────────────

    /// <summary>Adauga o intrare alimentara noua.</summary>
    public async Task AddFoodEntry(FoodEntry entry)
    {
        var url = $"{FirestoreBase}/foodEntries";
        var body = FoodEntryToFirestoreJson(entry);
        var resp = await PostAsync(url, body, useAuth: true);

        var doc = JsonDocument.Parse(resp);
        var name = doc.RootElement.GetProperty("name").GetString()!;
        entry.FirebaseId = name.Split('/').Last();
    }

    /// <summary>Returneaza intrarile de azi ale unui user.</summary>
    public async Task<List<FoodEntry>> GetTodayEntries()
    {
        var uid = App.CurrentUser?.FirebaseUid;
        if (string.IsNullOrEmpty(uid)) return [];

        var todayStart = DateTime.Today.ToString("yyyy-MM-dd") + "T00:00:00Z";
        var todayEnd = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00Z";

        var queryBody = JsonSerializer.Serialize(new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "foodEntries" } },
                where = new
                {
                    compositeFilter = new
                    {
                        op = "AND",
                        filters = new object[]
                        {
                            MakeFieldFilter("userId", "EQUAL",                uid,        "stringValue"),
                            MakeFieldFilter("date",   "GREATER_THAN_OR_EQUAL", todayStart, "timestampValue"),
                            MakeFieldFilter("date",   "LESS_THAN",             todayEnd,   "timestampValue")
                        }
                    }
                },
                orderBy = new[] { new { field = new { fieldPath = "date" }, direction = "DESCENDING" } }
            }
        });

        var url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
        var resp = await PostAsync(url, queryBody, useAuth: true);

        return ParseFoodEntries(resp);
    }

    /// <summary>
    /// Returneaza TOATE intrarile unui user, ordonate descrescator dupa data.
    /// Folosit de JournalPage pentru istoricul complet.
    /// </summary>
    public async Task<List<FoodEntry>> GetAllEntries()
    {
        var uid = App.CurrentUser?.FirebaseUid;
        if (string.IsNullOrEmpty(uid)) return [];

        var queryBody = JsonSerializer.Serialize(new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "foodEntries" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "userId" },
                        op = "EQUAL",
                        value = new { stringValue = uid }
                    }
                },
                orderBy = new[] { new { field = new { fieldPath = "date" }, direction = "DESCENDING" } }
            }
        });

        var url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
        var resp = await PostAsync(url, queryBody, useAuth: true);

        return ParseFoodEntries(resp);
    }

    /// <summary>Sterge o intrare alimentara dupa FirebaseId.</summary>
    public async Task DeleteFoodEntry(int id)
    {
        throw new NotSupportedException(
            "Foloseste DeleteFoodEntryByFirebaseId(string firebaseId).");
    }

    public async Task DeleteFoodEntryByFirebaseId(string firebaseId)
    {
        var url = $"{FirestoreBase}/foodEntries/{firebaseId}";
        await DeleteAsync(url);
    }

    // ── WEIGHT ENTRIES ─────────────────────────────────────────────────────

    /// <summary>
    /// Adauga o inregistrare de greutate pentru ziua de azi.
    /// Daca exista deja una pentru azi, o suprascrie.
    /// </summary>
    public async Task AddWeightEntry(WeightEntry entry)
    {
        var uid = App.CurrentUser?.FirebaseUid;
        if (string.IsNullOrEmpty(uid)) return;

        // Folosim data ca parte din ID ca sa fie unica pe zi
        var docId = $"{uid}_{entry.Date:yyyy-MM-dd}";
        var url = $"{FirestoreBase}/weightEntries/{docId}";

        var body = JsonSerializer.Serialize(new
        {
            fields = new
            {
                userId = Sv(uid),
                weight = Dv(entry.Weight),
                date = new { timestampValue = entry.Date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") }
            }
        });

        await PatchAsync(url, body);
        entry.FirebaseId = docId;
    }

    /// <summary>
    /// Verifica daca exista deja o inregistrare de greutate pentru ziua de azi.
    /// </summary>
    public async Task<WeightEntry?> GetTodayWeightEntry()
    {
        var uid = App.CurrentUser?.FirebaseUid;
        if (string.IsNullOrEmpty(uid)) return null;

        var docId = $"{uid}_{DateTime.Today:yyyy-MM-dd}";
        var url = $"{FirestoreBase}/weightEntries/{docId}";

        try
        {
            var json = await GetAsync(url);
            var doc = JsonDocument.Parse(json);
            var fields = doc.RootElement.GetProperty("fields");
            return new WeightEntry
            {
                FirebaseId = docId,
                UserId = uid,
                Weight = Gd(fields, "weight"),
                Date = Gt(fields, "date")
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returneaza toate inregistrarile de greutate ale userului, ordonate crescator dupa data.
    /// Folosit de JournalPage pentru graficul de progres.
    /// </summary>
    public async Task<List<WeightEntry>> GetAllWeightEntries()
    {
        var uid = App.CurrentUser?.FirebaseUid;
        if (string.IsNullOrEmpty(uid)) return [];

        var queryBody = JsonSerializer.Serialize(new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "weightEntries" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "userId" },
                        op = "EQUAL",
                        value = new { stringValue = uid }
                    }
                },
                orderBy = new[] { new { field = new { fieldPath = "date" }, direction = "ASCENDING" } }
            }
        });

        var url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
        var resp = await PostAsync(url, queryBody, useAuth: true);

        var result = new List<WeightEntry>();
        var docs = JsonDocument.Parse(resp);
        foreach (var item in docs.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("document", out var doc)) continue;
            var fields = doc.GetProperty("fields");
            var name = doc.GetProperty("name").GetString()!;
            result.Add(new WeightEntry
            {
                FirebaseId = name.Split('/').Last(),
                UserId = Gs(fields, "userId"),
                Weight = Gd(fields, "weight"),
                Date = Gt(fields, "date")
            });
        }
        return result;
    }

    // ── HELPERS FIRESTORE ──────────────────────────────────────────────────

    private async Task SetUserDocument(User user)
    {
        var url = $"{FirestoreBase}/users/{Uri.EscapeDataString(user.Username)}";
        var body = UserToFirestoreJson(user);
        await PatchAsync(url, body);
    }

    private static string UserToFirestoreJson(User user) =>
        JsonSerializer.Serialize(new
        {
            fields = new
            {
                username = Sv(user.Username),
                password = Sv(user.Password),
                firebaseUid = Sv(user.FirebaseUid ?? ""),
                weight = Dv(user.Weight),
                height = Dv(user.Height),
                targetWeight = Dv(user.TargetWeight),
                age = Iv(user.Age),
                sex = Sv(user.Sex ?? ""),
                activityLevel = Sv(user.ActivityLevel ?? ""),
                goalDate = new
                {
                    timestampValue = user.GoalDate.HasValue
                    ? user.GoalDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
                    : "1970-01-01T00:00:00Z"
                },
                isMaintenanceMode = new { booleanValue = user.IsMaintenanceMode }
            }
        });

    private static User FirestoreDocToUser(string json)
    {
        var doc = JsonDocument.Parse(json);
        var fields = doc.RootElement.GetProperty("fields");

        var goalDateRaw = Gt(fields, "goalDate");
        DateTime? goalDate = goalDateRaw.Year > 1970 ? goalDateRaw : null;

        bool isMaintenance = false;
        if (fields.TryGetProperty("isMaintenanceMode", out var mProp) &&
            mProp.TryGetProperty("booleanValue", out var bVal))
            isMaintenance = bVal.GetBoolean();

        return new User
        {
            Username = Gs(fields, "username"),
            Password = Gs(fields, "password"),
            FirebaseUid = Gs(fields, "firebaseUid"),
            Weight = Gd(fields, "weight"),
            Height = Gd(fields, "height"),
            TargetWeight = Gd(fields, "targetWeight"),
            Age = Gi(fields, "age"),
            Sex = Gs(fields, "sex"),
            ActivityLevel = Gs(fields, "activityLevel"),
            GoalDate = goalDate,
            IsMaintenanceMode = isMaintenance
        };
    }

    private static string FoodEntryToFirestoreJson(FoodEntry e) =>
        JsonSerializer.Serialize(new
        {
            fields = new
            {
                userId = Sv(App.CurrentUser?.FirebaseUid ?? ""),
                foodName = Sv(e.FoodName ?? ""),
                calories = Dv(e.Calories),
                protein = Dv(e.Protein),
                carbs = Dv(e.Carbs),
                fat = Dv(e.Fat),
                sugar = Dv(e.Sugar),
                date = new { timestampValue = e.Date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") }
            }
        });

    private static List<FoodEntry> ParseFoodEntries(string json)
    {
        var result = new List<FoodEntry>();
        var docs = JsonDocument.Parse(json);

        foreach (var item in docs.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("document", out var doc)) continue;
            var fields = doc.GetProperty("fields");
            var name = doc.GetProperty("name").GetString()!;

            result.Add(new FoodEntry
            {
                FirebaseId = name.Split('/').Last(),
                UserId = 0,
                FoodName = Gs(fields, "foodName"),
                Calories = Gd(fields, "calories"),
                Protein = Gd(fields, "protein"),
                Carbs = Gd(fields, "carbs"),
                Fat = Gd(fields, "fat"),
                Sugar = Gd(fields, "sugar"),
                Date = Gt(fields, "date")
            });
        }
        return result;
    }

    // ── VALUE BUILDERS ─────────────────────────────────────────────────────
    private static object Sv(string v) => new { stringValue = v };
    private static object Dv(double v) => new { doubleValue = v };
    private static object Iv(int v) => new { integerValue = v.ToString() };

    // ── VALUE EXTRACTORS ───────────────────────────────────────────────────
    private static string Gs(JsonElement f, string key) =>
        f.TryGetProperty(key, out var p) &&
        p.TryGetProperty("stringValue", out var s) ? s.GetString() ?? "" : "";

    private static double Gd(JsonElement f, string key)
    {
        if (!f.TryGetProperty(key, out var p)) return 0;
        if (p.TryGetProperty("doubleValue", out var d)) return d.GetDouble();
        if (p.TryGetProperty("integerValue", out var i))
            return double.TryParse(i.GetString(), out var r) ? r : 0;
        return 0;
    }

    private static int Gi(JsonElement f, string key)
    {
        if (!f.TryGetProperty(key, out var p)) return 0;
        if (p.TryGetProperty("integerValue", out var i))
            return int.TryParse(i.GetString(), out var r) ? r : 0;
        return 0;
    }

    private static DateTime Gt(JsonElement f, string key)
    {
        if (f.TryGetProperty(key, out var p) &&
            p.TryGetProperty("timestampValue", out var t) &&
            DateTime.TryParse(t.GetString(), out var dt))
            return dt.ToLocalTime();
        return DateTime.Now;
    }

    private static object MakeFieldFilter(string field, string op, string value, string type) =>
        new
        {
            fieldFilter = new
            {
                field = new { fieldPath = field },
                op = op,
                value = type == "timestampValue"
                    ? (object)new { timestampValue = value }
                    : new { stringValue = value }
            }
        };

    // ── HTTP HELPERS ───────────────────────────────────────────────────────
    private async Task<string> GetAsync(string url)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuth(req);
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }

    private async Task<string> PostAsync(string url, string body, bool useAuth = false)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (useAuth) AddAuth(req);
        var resp = await _http.SendAsync(req);
        var respBody = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Firebase error {resp.StatusCode}: {respBody}");
        return respBody;
    }

    private async Task<string> PatchAsync(string url, string body)
    {
        var req = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        AddAuth(req);
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }

    private async Task DeleteAsync(string url)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, url);
        AddAuth(req);
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    private void AddAuth(HttpRequestMessage req)
    {
        if (!string.IsNullOrEmpty(_idToken))
            req.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _idToken);
    }

    private static string UsernameToEmail(string username) =>
        $"{username.ToLower()}@calorielens.app";
}