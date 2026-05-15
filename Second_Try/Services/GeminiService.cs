using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Second_Try.Services
{
    public interface IGeminiService
    {
        Task<string> AskAsync(List<ChatMessage> history, string userMessage, string? userContext = null);
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "user";   // "user" or "model"
        public string Text { get; set; } = "";
    }

    public class GeminiService : IGeminiService
    {
        // ── API Key pool with round-robin + failover ──────────────
        private static readonly string[] _apiKeys =
        [
            "AIzaSyC7VX1dm79o9mzunNuIe-L85-ElZqy_6EI",
            "AIzaSyCfvvQ7SnPYWV2Ho2NA9IJGIm_bPot3IA8",
            "AIzaSyDdVzjjKkcfjk6yUQ7QD8zfe_ir49Xqo_c",
            "AIzaSyDwg_NG_VepMPe5UJamMox6WsTC2Gx-lyU",
            "AIzaSyA8AYnjlyqNrGuANCDZOepV1_I16KQ9YNI"
        ];

        private static int _keyIndex = 0;

        private const string ModelEndpoint =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent";

        // ── System prompt: knows about SRC Travel ─────────────────
        private const string SystemPrompt = @"
You are ARIA (Automated Route & Info Assistant), the friendly AI assistant for SRC Travel — an Online Bus Ticket Reservation System.

## About SRC Travel:
- SRC Travel is a premium bus ticket reservation platform serving Pakistan.
- Customers can register, log in, and submit booking requests online.
- Available bus types: Standard, Business, and Luxury (VIP).
- The system supports multiple routes across major Pakistani cities including Karachi, Lahore, Islamabad, Peshawar, Quetta, Multan, Faisalabad, Rawalpindi, and more.
- Fares vary by route and bus class and are set by admin.

## How Booking Works:
1. Register or log in as a customer.
2. Go to 'New Request' to submit a booking request (origin, destination, date, seats, bus type).
3. An employee reviews the request and either accepts or rejects it.
4. If accepted, a booking ticket is issued automatically.
5. Customers can view all their requests under 'My Requests'.
6. Pending requests can be cancelled by the customer.

## User Roles:
- **Customer**: Can book tickets, view history, manage profile.
- **Employee**: Reviews and processes booking requests, manages records.
- **Admin**: Full system management — employees, buses, routes, price list, reports.

## Key Features:
- Google OAuth login available for customers.
- Email notifications for booking accepted, rejected, cancelled, and welcome emails.
- Auto-expiry: Pending requests automatically expire if the travel date passes.
- Password reset via email link (30-minute expiry).

## Rules for your responses:
- Be friendly, concise, and helpful.
- If someone asks about pricing, tell them fares depend on the route and bus class and they should check the Price List on the website or ask an employee.
- If someone asks about real-time bus availability, explain that they should submit a booking request and the team will confirm.
- If user context (booking info) is provided, use it to answer personal questions.
- Do NOT make up specific prices or bus numbers unless provided in the user context.
- Always respond in the same language the user is writing in (Urdu/English).
- Keep responses short and useful — max 3-4 short paragraphs unless asked for detail.
";

        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IHttpClientFactory httpFactory, ILogger<GeminiService> logger)
        {
            _httpFactory = httpFactory;
            _logger      = logger;
        }

        public async Task<string> AskAsync(List<ChatMessage> history, string userMessage, string? userContext = null)
        {
            // Build the full system instruction
            string systemInstruction = SystemPrompt;
            if (!string.IsNullOrEmpty(userContext))
                systemInstruction += "\n\n## Current User's Booking Data:\n" + userContext;

            // Build contents array (history + new message)
            var contents = history
                .TakeLast(20) // limit to last 20 turns to avoid token overflow
                .Select(m => new
                {
                    role  = m.Role,
                    parts = new[] { new { text = m.Text } }
                })
                .ToList<object>();

            // Add the new user message
            contents.Add(new
            {
                role  = "user",
                parts = new[] { new { text = userMessage } }
            });

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents,
                generationConfig = new
                {
                    temperature     = 0.7,
                    maxOutputTokens = 600
                }
            };

            string json = JsonSerializer.Serialize(requestBody);

            // Try each key in order until one succeeds
            for (int attempt = 0; attempt < _apiKeys.Length; attempt++)
            {
                int idx = (Interlocked.Increment(ref _keyIndex) - 1) % _apiKeys.Length;
                string key = _apiKeys[idx];

                try
                {
                    var client   = _httpFactory.CreateClient();
                    var content  = new StringContent(json, Encoding.UTF8, "application/json");
                    var url      = $"{ModelEndpoint}?key={key}";
                    var response = await client.PostAsync(url, content);
                    
                    string responseJson = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogWarning("Gemini key #{Idx} hit rate limit, trying next key.", idx + 1);
                            continue; // try next key
                        }
                        
                        _logger.LogError("Gemini API Error (Status: {StatusCode}): {Error}", response.StatusCode, responseJson);
                        throw new Exception($"Gemini API Error: {response.StatusCode}");
                    }

                    using var doc = JsonDocument.Parse(responseJson);

                    string text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "Sorry, I couldn't generate a response.";

                    return text.Trim();
                }
                catch (Exception ex) when (attempt < _apiKeys.Length - 1)
                {
                    _logger.LogWarning(ex, "Gemini key #{Idx} failed, trying next.", idx + 1);
                    continue;
                }
                catch (Exception ex) when (attempt == _apiKeys.Length - 1)
                {
                    _logger.LogError(ex, "All Gemini API keys failed.");
                }
            }

            return "I'm having trouble connecting right now. Please try again in a moment.";
        }
    }
}
