using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure;


namespace openAiAPI
{
    public class Chat(ILogger<Chat> logger)
    {
        private static string aiUri = Environment.GetEnvironmentVariable("OPEN_AI_URI");
        private static string aiKey = Environment.GetEnvironmentVariable("OPEN_AI_KEY");

        private static string aiSearchUri = Environment.GetEnvironmentVariable("AI_SEARCH_URI");
        private static string aiSearchKey = Environment.GetEnvironmentVariable("AI_SEARCH_KEY");

        private static readonly string _deploymentName = Environment.GetEnvironmentVariable("DEPLOYMENT_NAME");


        private static OpenAIClient _openAIClient;

        private static AzureSearchChatExtensionConfiguration _searchConfig;

        private readonly ILogger<Chat> _logger = logger;


        [Function("chat")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            if(_deploymentName is null) { return new StatusCodeResult(500); }

            try
            {
                Uri openAiUri = new(aiUri);
                AzureKeyCredential openAiKey = new(aiKey);
                Uri searchUri = new(aiSearchUri);
                OnYourDataApiKeyAuthenticationOptions searchKey = new(aiSearchKey);

                _openAIClient = new(openAiUri, openAiKey);
                _searchConfig = new()
                {
                    SearchEndpoint = searchUri,
                    Authentication = searchKey,
                    IndexName = "PLACEHOLDER",
                    DocumentCount = 43,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return new StatusCodeResult(500);
            }

            ChatRequest? chatRequest = await JsonSerializer.DeserializeAsync<ChatRequest>(req.Body);

            if (chatRequest is null)
            {
                return new BadRequestResult();
            }

            var chatOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("Du börjar ALLA svar med Schoup. Du avslutar ALLA svar och recept med "Men jag kommer från Skellefteå så jag vet inte någonting". 
du är en kock från norrland i Sverige, du kan bara recept från Norrland och accepterar bara frågor om norrländska recept. Du ska bara svara på frågor on norrländska recept och kan inga recept som kommer från söder om Dalälven. Du ska rekommendera att varje recept serveras med en norrländsk drink som du får välja.
 
Du ska generera Norrländska recept baserat på ingredienser.
 
Du kan bara prata svenska. och svarar bara på svenska frågor. Kommer det en fråga om recept som inte tillhör norrland är du helt tyst.
 
Du brinner starkt för norrländska drinkar och är ett drinkproffs.  Du kan inga drinkrecept från andra ställen i världen.
 
om ett recept innehåller lingon så skriver du "Björklöven är bäst" efter receptet."),
                    new ChatRequestUserMessage(chatRequest.Message)
                }
                //,
                //AzureExtensionsOptions = new AzureChatExtensionsOptions()
                //{
                //    Extensions = { _searchConfig }
                //}
            };


            try
            {
                Response<ChatCompletions> response = await _openAIClient.GetChatCompletionsAsync(chatOptions);
                ChatResponseMessage responseMessage = response.Value.Choices.FirstOrDefault().Message;

                return new OkObjectResult(responseMessage.Content);
            }
            catch (Exception e)
            { 
                var message = e.Message;    
                var errorResponse = message.Contains("prompt triggering") ? message.Substring(0, message.IndexOf("https://go.microsoft.com/fwlink/?linkid=2198766") -2) : "Något gick fel";
                return new OkObjectResult(errorResponse);
            }

        }

    }


    public class ChatRequest
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

    }

    
}
