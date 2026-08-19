using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BIMHubPlugin.Models;
using Newtonsoft.Json;

namespace BIMHubPlugin.Services
{
    /// <summary>
    /// Логин через AD-авторизацию BimHelpDesk (POST /api/auth/login) — раздел 7.2/10 плана
    /// объединения: полноценный логин вместо статического токена в открытом config.json.
    /// </summary>
    public static class AuthApiClient
    {
        public static async Task<LoginResult> LoginAsync(string apiBaseUrl, string username, string password)
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            {
                // longLived: true — просим сервер выпустить токен на 30 дней (JwtSettings:PluginExpiresMinutes)
                // вместо обычных 2 часов веб-сессии. Токен кэшируется через TokenStore (DPAPI), поэтому
                // логин по AD нужен один раз, а не каждые пару часов посреди рабочего дня в Revit.
                var body = JsonConvert.SerializeObject(new { username, password, longLived = true });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var url = $"{apiBaseUrl.TrimEnd('/')}/auth/login";
                SimpleLogger.Log($"AuthApiClient.LoginAsync: POST {url}");

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(url, content);
                }
                catch (Exception ex)
                {
                    SimpleLogger.Error("AuthApiClient.LoginAsync: request failed", ex);
                    throw new Exception($"Не удалось подключиться к серверу: {ex.Message}", ex);
                }

                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string message = "Неверный логин или пароль";
                    try
                    {
                        var err = JsonConvert.DeserializeAnonymousType(json, new { message = "" });
                        if (err != null && !string.IsNullOrEmpty(err.message))
                            message = err.message;
                    }
                    catch { /* тело не JSON-объект с message — используем сообщение по умолчанию */ }

                    SimpleLogger.Log($"AuthApiClient.LoginAsync: failed, status {response.StatusCode}, message '{message}'");
                    throw new Exception(message);
                }

                var result = JsonConvert.DeserializeObject<LoginResult>(json);
                SimpleLogger.Log($"AuthApiClient.LoginAsync: success for '{result?.Username}'");
                return result;
            }
        }
    }
}
