using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CalorieLens.Services
{
    public static class FoodAIService
    {
        private const string ApiKey = "YOUR_GEMINI_API_KEY_HERE";
        private const string ApiUrl =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        private const int MaxRetries = 3;
        private const int RetryDelayMs = 3000; // 3 secunde intre retry-uri

        public static async Task<string> AnalyzeFood(string imagePath)
        {
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
            string base64Image = Convert.ToBase64String(imageBytes);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = base64Image
                                }
                            },
                            new
                            {
                                text = "Analizeaza aceasta imagine cu mancare. " +
                                       "Identifica ce mancare este si estimeaza caloriile. " +
                                       "Raspunde in romana in formatul:\n" +
                                       "Mancare: [nume]\n" +
                                       "Calorii estimate: [numar] kcal\n" +
                                       "Proteine: ~[numar]g\n" +
                                       "Carbohidrati: ~[numar]g\n" +
                                       "Grasimi: ~[numar]g\n" +
                                       "Nota: [observatii scurte]"
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync($"{ApiUrl}?key={ApiKey}", content);

                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode == (System.Net.HttpStatusCode)429)
                    {
                        if (attempt < MaxRetries)
                        {
                            await Task.Delay(RetryDelayMs * attempt); // 3s, 6s, 9s
                            continue;
                        }
                        return "Serverul Gemini este supraîncărcat momentan. Încearcă din nou peste câteva secunde.";
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        return $"Eroare API ({response.StatusCode}): {error}";
                    }

                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);

                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return text ?? "Nu s-a putut obtine un raspuns.";
                }
                catch (TaskCanceledException)
                {
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(RetryDelayMs);
                        continue;
                    }
                    return "Timeout — serverul nu a raspuns in 30 de secunde. Incearca din nou.";
                }
                catch (Exception ex)
                {
                    return $"Eroare: {ex.Message}";
                }
            }

            return "Nu s-a putut contacta serverul dupa 3 incercari.";
        }
    }
}