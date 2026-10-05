using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BIMHubPlugin.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BIMHubPlugin.Services
{
    public static class AuthApiClient
    {
        public static async Task<LoginResult> LoginAsync(string apiBaseUrl, string username, string password, CancellationToken ct = default)
        {
            var endpoint = ApiEndpoint.Normalize(apiBaseUrl);
            using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) })
            using (var content = new StringContent(JsonConvert.SerializeObject(new { username, password, longLived = true }), Encoding.UTF8, "application/json"))
            using (var response = await client.PostAsync(endpoint + "/auth/login", content, ct).ConfigureAwait(false))
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                if (!response.IsSuccessStatusCode)
                {
                    var message = "Не удалось выполнить вход (" + (int)response.StatusCode + ").";
                    try
                    {
                        var error = JObject.Parse(json);
                        message = (string)error["message"] ?? (string)error["title"] ?? message;
                    }
                    catch (JsonException) { }
                    throw new InvalidOperationException(message);
                }
                var result = JsonConvert.DeserializeObject<LoginResult>(json);
                if (string.IsNullOrWhiteSpace(result?.Token)) throw new InvalidOperationException("Сервер не вернул токен сессии.");
                return result;
            }
        }
    }
}
