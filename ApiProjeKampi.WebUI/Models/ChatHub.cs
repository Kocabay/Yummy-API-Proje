using Microsoft.AspNetCore.SignalR;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiProjeKampi.WebUI.Models
{
    // ChatHub: SignalR üzerinden gerçek zamanlı mesaj akışı sağlayan hub sınıfı.
    // Amaç: Kullanıcıdan mesaj alıp, OpenAI API'ye akış modunda gönderip
    // token token geri bildirim vermek.

    public class ChatHub : Hub
    {
        private const string apiKey = "";                       // OpenAI API anahtarınız
        private const string modelGpt = "gpt-4o-mini";           // Kullanılacak model
        private readonly IHttpClientFactory _httpClientFactory;  // HttpClient üreticisi

        // Constructor -> IHttpClientFactory DI (dependency injection) ile alınır
        public ChatHub(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Her bağlantı için mesaj geçmişini tutan dictionary
        private static readonly Dictionary<string, List<Dictionary<string, string>>> _history = new();

        // Kullanıcı hub’a bağlandığında çalışır
        public override Task OnConnectedAsync()
        {
            // Sisteme başlangıç mesajı eklenir
            _history[Context.ConnectionId] = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    ["role"] = "system",
                    ["content"] = "You are a helpful assistant. Keep answers concise."
                }
            };

            return base.OnConnectedAsync();
        }

        // Kullanıcı bağlantıdan ayrıldığında geçmişi silinir
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _history.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        // İstemci mesaj gönderdiğinde tetiklenen metod
        public async Task SendMessage(string userMessage)
        {
            // Gönderdiği mesajı kullanıcıya geri gönder (ekranda göstermek için)
            await Clients.Caller.SendAsync("ReceiveUserEcho", userMessage);

            // Bağlantıya ait geçmişi al
            var history = _history[Context.ConnectionId];

            // Mesajı geçmişe ekle
            history.Add(new() { ["role"] = "user", ["content"] = userMessage });

            // OpenAI’ye akışlı (stream) istek yap
            await StreamOpenAI(history, Context.ConnectionAborted);
        }

        // OpenAI Chat Completion STREAM isteği
        public async Task StreamOpenAI(List<Dictionary<string, string>> history, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("openai");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // API'ye gönderilecek payload
            var payload = new
            {
                model = modelGpt,
                messages = history,   // Tüm konuşma geçmişi
                stream = true,        // Token token akış modunda cevap alınacak
                temperature = 0.2     // Daha tutarlı cevaplar için düşük sıcaklık
            };

            // HTTP isteğinin hazırlanması
            using var req = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // Yanıtı header'lar gelince almaya başla
            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            resp.EnsureSuccessStatusCode();

            // Stream okuma işlemleri
            using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var sb = new StringBuilder();  // Tam cevabı biriktirmek için

            // Stream satır satır okunur
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data:")) continue; // Stream formatı "data:" ile başlar

                var data = line["data:".Length..].Trim();

                if (data == "[DONE]") break; // OpenAI tamamlandı bilgisini verdi

                try
                {
                    // Stream datasını modele deserialize et
                    var chunk = JsonSerializer.Deserialize<ChatStreamChunk>(data);
                    var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;

                    // Token geldiyse hem ekrana gönder hem biriktir
                    if (!string.IsNullOrEmpty(delta))
                    {
                        sb.Append(delta);

                        // Token'ı anlık olarak frontend’e gönder
                        await Clients.Caller.SendAsync("ReceiveToken", delta, cancellationToken);
                    }
                }
                catch
                {
                    // Hata olsa bile akışı kesme
                }
            }

            // Tam cevap
            var full = sb.ToString();

            // Geçmişe asistandan gelen mesaj olarak ekle
            history.Add(new() { ["role"] = "assistant", ["content"] = full });

            // Son mesajı kullanıcıya ilet (tamamlandığında tetiklenir)
            await Clients.Caller.SendAsync("CompleteMessage", full, cancellationToken);
        }

        // === STREAM JSON MODELLERİ ===

        // OpenAI stream chunk modeli

        //C# sealed Nedir? sealed, bir sınıfın veya bir metodun miras alınmasını(inheritance) veya override edilmesini engelleyen bir anahtar kelimedir.
        private sealed class ChatStreamChunk
        {
            [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
        }

        private sealed class Choice
        {
            [JsonPropertyName("delta")] public Delta? Delta { get; set; }
            [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
        }

        private sealed class Delta
        {
            [JsonPropertyName("content")] public string? Content { get; set; }
            [JsonPropertyName("role")] public string? Role { get; set; }
        }
    }
}