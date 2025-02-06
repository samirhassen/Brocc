using Newtonsoft.Json;
using NTech.Ai.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Core.Host.Services.AiHelpServices
{
    public class WikiService
    {
        private readonly string userEmail;
        private readonly string userApiToken;
        private readonly string baseAddress;

        public WikiService(string userEmail, //Same user as the api token
            string userApiToken, //From: https://id.atlassian.com/manage-profile/security/api-tokens
            string baseAddress) //Like https://<your-company>.atlassian.net)
        {
            this.userEmail = userEmail;
            this.userApiToken = userApiToken;
            this.baseAddress = baseAddress;
        }
        
        public async Task<string> GetDocumentationTextCached(string pageId, bool includeChildPages = false)
        {
            if (BackofficeHelpQueryFunction.DocumenationCache.TryGetValue(pageId, out var text))
                return text;

            var documentationText = await GetDocumentationText(pageId, includeChildPages);
            BackofficeHelpQueryFunction.DocumenationCache[pageId] = documentationText;
            return documentationText;
        }

        private async Task<string> GetDocumentationText(string pageId, bool includeChildPages = false)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new NTechBasicAuthenticationHeaderValue(userEmail, userApiToken);
            client.BaseAddress = new Uri(baseAddress);
            var mainPageContent = await GetPage(pageId, client, 0, includeChildPages);
            return mainPageContent;
        }

        private async Task<string> GetPage(string pageId, HttpClient client, int depth, bool includeChildPages = false)
        {
            if (depth > 5)
                throw new Exception("Depth > 5 for page: " + pageId);

            //Get body content
            var result = await client.GetAsync($"wiki/rest/api/content/{pageId}?expand=body.view");
            result.EnsureSuccessStatusCode();
            var bodyContent = await result.Content.ReadAsStringAsync();
            var parsedBodyContent = JsonConvert.DeserializeAnonymousType(bodyContent, new { id = "", type = "", status = "", body = new { view = new { value = "" } } });
            if (!(parsedBodyContent?.id == pageId && parsedBodyContent?.type == "page" && parsedBodyContent?.status == "current"))
                throw new Exception($"Invalid page: {pageId}");
            var pageHtmlDocument = new HtmlAgilityPack.HtmlDocument();
            pageHtmlDocument.LoadHtml(parsedBodyContent.body.view.value);
            var pageHtml = string.Join(" ", pageHtmlDocument.DocumentNode.SelectNodes("//text()").Select(x => x.InnerText));

            //Get child pages
            if (includeChildPages)
            {
                var childPagesResult = await client.GetAsync($"wiki/rest/api/content/{pageId}/child/page");
                //NOTE: This likely has paging that we are ignoring now
                result.EnsureSuccessStatusCode();
                var childPagesContent = await childPagesResult.Content.ReadAsStringAsync();
                var parsedChildsContent = JsonConvert.DeserializeAnonymousType(childPagesContent, new { results = new[] { new { id = "", type = "", status = "" } } });
                var childPageIds = new List<string>();
                foreach (var cr in parsedChildsContent!.results)
                {
                    if (!(cr?.type == "page" && cr?.status == "current"))
                        throw new Exception($"Invalid child page on: {pageId}");
                    childPageIds.Add(cr.id);
                }

                foreach (var childPageId in childPageIds)
                {
                    var childPageHtml = await GetPage(childPageId, client, depth + 1);
                    pageHtml += Environment.NewLine + Environment.NewLine + childPageHtml;
                }
            }

            return pageHtml;
        }

        public async Task<List<string>> GetAllChildPageIds(string pageId)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new NTechBasicAuthenticationHeaderValue(userEmail, userApiToken);
            client.BaseAddress = new Uri(baseAddress);

            var childPagesResult = await client.GetAsync($"wiki/rest/api/content/{pageId}/child/page");
            //NOTE: This likely has paging that we are ignoring now
            childPagesResult.EnsureSuccessStatusCode();
            var childPagesContent = await childPagesResult.Content.ReadAsStringAsync();
            var parsedChildsContent = JsonConvert.DeserializeAnonymousType(childPagesContent, new { results = new[] { new { id = "", type = "", status = "" } } });
            var childPageIds = new List<string>();
            foreach (var cr in parsedChildsContent!.results)
            {
                if (!(cr?.type == "page" && cr?.status == "current"))
                    throw new Exception($"Invalid child page on: {pageId}");
                childPageIds.Add(cr.id);
            }

            return childPageIds;
        }

        private class NTechBasicAuthenticationHeaderValue : AuthenticationHeaderValue
        {
            public NTechBasicAuthenticationHeaderValue(string userName, string password)
                : base("Basic", EncodeCredential(userName, password))
            {
            }

            private static string EncodeCredential(string userName, string password)
            {
                Encoding uTF = Encoding.UTF8;
                string s = $"{userName}:{password}";
                return Convert.ToBase64String(uTF.GetBytes(s));
            }
        }
    }
}
