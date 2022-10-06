using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CallingBotSample.Utility
{
    public static class SpeachApiUtils
    {
        private static async Task<string> GetAccessToken(string subscriptionKey)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var response = await client.PostAsync("https://westus.api.cognitive.microsoft.com/sts/v1.0/issuetoken", null);
            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<Tuple<string, string>> GenerateTextToSpeechFile(string message, BotOptions botOptions)
        {
            var filename = Guid.NewGuid();

            var accessToken = await GetAccessToken(botOptions.SpeechSubscriptionKey);

            string host = "https://westus.tts.speech.microsoft.com/cognitiveservices/v1";

            // Create SSML document.
            var xmlMessage = string.Format(
                "<speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xmlns:mstts='https://www.w3.org/2001/mstts' xmlns:emo='http://www.w3.org/2009/10/emotionml' version='1.0' xml:lang='en-US'>" +
                    "<voice name='en-US-JennyNeural'>" +
                        "<prosody rate='0%' pitch='0%'>{0}</prosody>" +
                    "</voice>" +
                "</speak>", message);

            using (HttpClient client = new HttpClient())
            {
                using (HttpRequestMessage request = new HttpRequestMessage())
                {
                    // Set the HTTP method
                    request.Method = HttpMethod.Post;

                    // Construct the URI
                    request.RequestUri = new Uri(host);

                    // Set the content type header
                    request.Content = new StringContent(xmlMessage, Encoding.UTF8, "application/ssml+xml");

                    // Set additional header, such as Authorization and User-Agent
                    request.Headers.Add("Authorization", "Bearer " + accessToken);
                    request.Headers.Add("Connection", "Keep-Alive");

                    // Update your resource name
                    request.Headers.Add("User-Agent", "CallingBotSample");

                    // Audio output format. See API reference for full list.
                    request.Headers.Add("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");

                    request.Headers.Add("Content-Type", "audio/wav; codecs=audio/pcm; samplerate=16000");

                    // Create a request
                    using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        //response.EnsureSuccessStatusCode();

                        // Asynchronously read the response
                        using (Stream dataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            using (FileStream fileStream = new FileStream(@"wwwroot/audio/" + filename + ".wav", FileMode.Create, FileAccess.Write, FileShare.Write))
                            {
                                await dataStream.CopyToAsync(fileStream).ConfigureAwait(false);
                                fileStream.Close();
                            }
                        }
                    }
                }
            }

            return new Tuple<string, string>("audio/" + filename + ".wav", filename + ".wav");
        }
    }
}
