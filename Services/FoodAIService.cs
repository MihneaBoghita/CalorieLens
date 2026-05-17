using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CalorieLens.Services
{
    public static class FoodAIService
    {
        private const string ApiKey = "YOUR_GEMINI_API_KEY_HERE";

        private const string ApiUrl =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

        public static async Task<string> AnalyzeFood(string imagePath)
        {
            try
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

                using var httpClient = new HttpClient();
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{ApiUrl}?key={ApiKey}", content);

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
            catch (Exception ex)
            {
                return $"Eroare: {ex.Message}";
            }
        }
    }
}