using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NTech.Core.Host.Services.AiHelpServices;
using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;

namespace NTech.Ai.Api
{
    public class BackofficeHelpQueryFunction
    {
        private static ConcurrentDictionary<string, AiHelpSession> sessionCache = new ConcurrentDictionary<string, AiHelpSession>();
        public static ConcurrentDictionary<string, string> DocumenationCache = new ConcurrentDictionary<string, string>();        

        [FunctionName("backoffice-help-query")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                   .SetBasePath(context.FunctionAppDirectory)
                   .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables()
                   .Build();

            string GetNTechAiSetting(string name)
            {
                var value = config.GetSection(name)?.Value;
                if (value == null)
                    throw new Exception("Missing setting " + name);
                return value;
            }

            if(GetNTechAiSetting("IsBackOfficeHelpDisabled") == Boolean.TrueString)
                return new OkObjectResult(new
                {
                    Id = (string)null,
                    Answer = "<p>The help system is currently disabled.</p>",
                    IsComplete = true
                });

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            AiHelpRequest request = JsonConvert.DeserializeObject<AiHelpRequest>(requestBody);            

            if(!string.IsNullOrWhiteSpace(request.OngoingQueryId))
            {
                sessionCache.TryGetValue(request.OngoingQueryId, out var ongoingSession);
                if(ongoingSession?.IsComplete == true)
                    sessionCache.TryRemove(ongoingSession.Id, out var _);
                
                return new OkObjectResult(new
                {
                    Id = ongoingSession?.Id ?? request.OngoingQueryId,
                    Answer = ongoingSession?.QueryResponse ?? "<p>Query failed. Please try again later.</p>",
                    IsComplete = ongoingSession?.IsComplete ?? true
                });
            }

            var wikiService = new WikiService(GetNTechAiSetting("WikiEmail"), GetNTechAiSetting("WikiApiToken"), GetNTechAiSetting("WikiBaseAddress"));
            var session = new AiHelpSession
            {
                Id = Guid.NewGuid().ToString(),
                IsComplete = false,
                QueryResponse = ""
            };
            sessionCache[session.Id] = session;

            try
            {
                var client = new OpenAIClient(new Uri(GetNTechAiSetting("OpenAiApiBase")), new AzureKeyCredential(GetNTechAiSetting("OpenAiApiKey")));

                var docs = "";
                if (GetNTechAiSetting("IsEmbeddingEnabled") != Boolean.TrueString)
                {
                    docs = await wikiService.GetDocumentationTextCached(GetNTechAiSetting("WikiPageId"), true);
                }
                else
                {
                    var pageIds = await wikiService.GetAllChildPageIds(GetNTechAiSetting("WikiPageId"));
                    var embeddingService = new EmbeddingService(wikiService, client, GetNTechAiSetting("OpenAiEmbeddingEngineName"));
                    var userQuestionEmbedding = await embeddingService.GetEmbeddingAsync(request.NewQuery);

                    foreach (var pageId in pageIds)
                    {
                        var pageContent = await wikiService.GetDocumentationTextCached(pageId);
                        var mostSimilarPart = await embeddingService.FindMostSimilarPartAsync(client, userQuestionEmbedding, pageContent);
                        docs += mostSimilarPart;
                    }
                }
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                //Keeps running in the background. The caller will poll this function with OngoingQueryId to get partial results of from the ai until IsComplete = true
                ProcessQueryInTheBackground(request.NewQuery, docs, session, GetNTechAiSetting, client);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            catch (Exception)
            {
                var exceptionAnswer = "Whoops, something went wrong! Give me a minute and try again.";
                session.QueryResponse = $"<p>{exceptionAnswer}</p>";
                session.IsComplete = true;
            }

            return new OkObjectResult(new
            {
                Id = session.Id,
                Answer = "",
                IsComplete = false
            });
        }

        private static async Task ProcessQueryInTheBackground(string query, string documentationText, AiHelpSession session, Func<string, string> getSetting, OpenAIClient client)
        {
            try
            {
                var m = new ChatCompletionsOptions();
                m.Messages.Add(new ChatMessage(ChatRole.System, "You work for Näktergal. You are a support agent trying to help the users of a loan system. Format your answers in html to make it more readable. Lists have breaks between each line. Divide up text into seperate chapters if its long enough. You have this document as a reference:"));
                m.Messages.Add(new ChatMessage(ChatRole.System, documentationText));
                m.Messages.Add(new ChatMessage(ChatRole.User, query));

                var chatCompletionsResponse = await client.GetChatCompletionsStreamingAsync(getSetting("OpenAiEngineName"), m);

                await foreach (var chatChoice in chatCompletionsResponse.Value.GetChoicesStreaming())
                {
                    await foreach (var chatMessage in chatChoice.GetMessageStreaming())
                    {
                        session.QueryResponse += chatMessage.Content;
                    }
                }
                session.IsComplete = true;

            }
            catch (Exception)
            {
                var exceptionAnswer = "Whoops, something went wrong! Give me a minute and try again.";
                session.QueryResponse = $"<p>{exceptionAnswer}</p>";
                session.IsComplete = true;
            }
        }
    }

    public class AiHelpRequest
    {        
        public string NewQuery { get; set; }
        public string OngoingQueryId { get; set; }
    }

    public class AiHelpSession
    {
        public string Id { get; set; }
        public string QueryResponse { get; set; }
        public bool IsComplete { get; set; }
    }

    public class AiSettings
    {
        public string OpenAiApiBase { get; set; }
        public string OpenAiApiKey { get; set; }
        public string OpenAiEngineName { get; set; }
        public string OpenAiEmbeddingEngineName { get; set; }
        public string WikiPageId { get; set; }
        public string WikiEmail { get; set; }
        public string WikiApiToken { get; set; }
        public string WikiBaseAddress { get; set; }
        public bool IsEmbeddingEnabled { get; set; }
    }
}