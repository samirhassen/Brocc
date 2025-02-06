using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Core.Host.Services.AiHelpServices
{
    public class EmbeddingService
    {
        private readonly WikiService wikiService;
        private readonly OpenAIClient client;
        private readonly string azureEmbeddingEngineName;

        public EmbeddingService(WikiService wikiService, OpenAIClient client, string azureEmbeddingEngineName)
        {
            this.wikiService = wikiService;
            this.client = client;
            this.azureEmbeddingEngineName = azureEmbeddingEngineName;
        }

        public async Task<double[]> GetEmbeddingAsync(string text)
        {
            var embeddingResponse = await client.GetEmbeddingsAsync(azureEmbeddingEngineName, new EmbeddingsOptions(text));
            var embedding = embeddingResponse.Value.Data[0].Embedding.Select(x => (double)x).ToArray();

            return embedding;
        }

        //https://github.com/openai/openai-cookbook/blob/main/examples/Question_answering_using_embeddings.ipynb
        //It might be better to do this so that we find the most similar pageId instead
        public async Task<string> FindMostSimilarPartAsync(OpenAIClient client,
        double[] queryEmbedding,
        string bigText)
        {
            const int windowSize = 500; // Adjust the window size as needed

            // Split the big text into chunks of the specified window size
            List<string> chunks = SplitTextIntoChunks(bigText, windowSize);

            string mostSimilarPart = null;
            double maxRelatedness = double.MinValue;

            foreach (string chunk in chunks)
            {
                var x = await client.GetEmbeddingsAsync(azureEmbeddingEngineName, new EmbeddingsOptions(chunk));
                double[] chunkEmbedding = x.Value.Data[0].Embedding.Select(x => (double)x).ToArray(); 
                double relatedness = CalculateCosineSimilarity(queryEmbedding, chunkEmbedding);

                if (relatedness > maxRelatedness)
                {
                    maxRelatedness = relatedness;
                    mostSimilarPart = chunk;
                }
            }

            return mostSimilarPart;
        }

        public double CalculateCosineSimilarity(double[] vectorA, double[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                throw new ArgumentException("Vector dimensions must match.");

            double dotProduct = 0.0;
            double magnitudeA = 0.0;
            double magnitudeB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            magnitudeA = Math.Sqrt(magnitudeA);
            magnitudeB = Math.Sqrt(magnitudeB);

            double cosineSimilarity = dotProduct / (magnitudeA * magnitudeB);
            return cosineSimilarity;
        }

        private static List<string> SplitTextIntoChunks(string text, int chunkSize)
        {
            List<string> chunks = new List<string>();
            int textLength = text.Length;
            int numChunks = (textLength + chunkSize - 1) / chunkSize;

            for (int i = 0; i < numChunks; i++)
            {
                int startIndex = i * chunkSize;
                int endIndex = Math.Min(startIndex + chunkSize, textLength);
                string chunk = text.Substring(startIndex, endIndex - startIndex);
                chunks.Add(chunk);
            }

            return chunks;
        }
    }
}
