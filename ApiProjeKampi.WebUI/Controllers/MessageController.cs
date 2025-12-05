using ApiProjeKampi.WebUI.Dtos.MessageDtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static ApiProjeKampi.WebUI.Controllers.AIController;

namespace ApiProjeKampi.WebUI.Controllers
{
    public class MessageController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public MessageController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> MessageList()
        {
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("https://localhost:7177/api/Messages");
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultMessageDto>>(jsonData);
                return View(values);
            }
            return View();
        }

        [HttpGet]
        public IActionResult CreateMessage()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(CreateMessageDto createMessageDto)
        {
            var client = _httpClientFactory.CreateClient();
            createMessageDto.SendDate = DateTime.Now;
            var jsonData = JsonConvert.SerializeObject(createMessageDto);
            StringContent stringContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var responseMessage = await client.PostAsync("https://localhost:7177/api/Messages", stringContent);
            if (responseMessage.IsSuccessStatusCode)
            {
                return RedirectToAction("MessageList");
            }
            return View();
        }

        public async Task<IActionResult> DeleteMessage(int id)
        {
            var client = _httpClientFactory.CreateClient();
            await client.DeleteAsync("https://localhost:7177/api/Messages?id=" + id);
            return RedirectToAction("MessageList");
        }

        [HttpGet]
        public async Task<IActionResult> UpdateMessage(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("https://localhost:7177/api/Messages/GetMessage?id=" + id);
            var jsonData = await responseMessage.Content.ReadAsStringAsync();
            var value = JsonConvert.DeserializeObject<GetMessageByIdDto>(jsonData);
            return View(value);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateMessage(UpdateMessageDto updateMessageDto)
        {
            var client = _httpClientFactory.CreateClient();
            var jsonData = JsonConvert.SerializeObject(updateMessageDto);
            StringContent stringContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
            await client.PutAsync("https://localhost:7177/api/Messages/", stringContent);
            return RedirectToAction("MessageList");
        }
        [HttpGet]
        public async Task<IActionResult> AsnwerMessageWithOpenAI(int id, string prompt)
        {
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("https://localhost:7177/api/Messages/GetMessage?id=" + id);
            var jsonData = await responseMessage.Content.ReadAsStringAsync();
            var value = JsonConvert.DeserializeObject<GetMessageByIdDto>(jsonData);
            prompt = value.MessageDetails;

            var apiKey = "";

            using var client2 = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestData = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new
                    {
                        role="system",
                        content="Sen bir restoran için kullanıcıların göndermiş olduğu mesajları detaylı ve olabildiğince müşteri memnuniyeti gözeten bir yapay zeka aracısın. Amacımız kullanıcı tarafından gönderilen mesajlara en olumlu ve en mantıklı mesajları göndermek."
                    },
                    new
                    {
                        role="user",
                        content= prompt
                    }
                },
                temperature = 0.5
            };

            var response = await client2.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>();
                var content = result.choices[0].message.content;
                ViewBag.answerAI = content;
            }
            else
            {
                ViewBag.answerAI = "Bir hata oluştu: " + response.StatusCode;
            }

            return View(value);
        }

        public PartialViewResult SendMessage()
        {
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(CreateMessageDto createMessageDto)
        {
            //Türkçe mesajı ingilizceye çevir

            var client = new HttpClient();
            var apiKey = "";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            try
            {
                var translateRequestBody = new
                {
                    inputs = createMessageDto.MessageDetails,
                };
                var translateJson = System.Text.Json.JsonSerializer.Serialize(translateRequestBody);
                var translateContent = new StringContent(translateJson, Encoding.UTF8, "application/json");
                var translateResponse = await client.PostAsync("https://api-inference.huggingface.co/models/Helsinki-NLP/opus-mt-tr-en", translateContent);
                var transleteResponseString = await translateResponse.Content.ReadAsStringAsync();
                string englishText = createMessageDto.MessageDetails;
                if (transleteResponseString.TrimStart().StartsWith("["))
                {
                    var translateDoc = JsonDocument.Parse(transleteResponseString);
                    englishText = translateDoc.RootElement[0].GetProperty("translation_text").GetString();
                    ViewBag.v = englishText;
                } 
            }
            catch 
            {

                throw;
            }


            var client2 = _httpClientFactory.CreateClient();
            createMessageDto.SendDate = DateTime.Now;
            var jsonData = JsonConvert.SerializeObject(createMessageDto);
            StringContent stringContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var responseMessage = await client2.PostAsync("https://localhost:7177/api/Messages", stringContent);
            if (responseMessage.IsSuccessStatusCode)
            {
                return RedirectToAction("MessageList");
            }
            return View();



        }

    }
}
