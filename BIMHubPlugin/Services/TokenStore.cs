using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace BIMHubPlugin.Services
{
    /// <summary>Закэшированный JWT + к какому ApiBaseUrl он относится (чтобы смена сервера не подставляла чужой токен).</summary>
    public class CachedToken
    {
        public string ApiBaseUrl { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Локальный кэш JWT-токена, зашифрованный через Windows DPAPI (CurrentUser scope) —
    /// раздел 7.2/10 плана объединения: токен не должен лежать на диске открытым текстом,
    /// как было в старом config.json. Расшифровать может только тот же Windows-пользователь
    /// на той же машине.
    /// </summary>
    public static class TokenStore
    {
        private static readonly string TokenPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BIMHubPlugin",
            "token.dat"
        );

        // Доп. энтропия — не секрет, просто снижает шанс случайной путаницы с другими
        // DPAPI-блобами этого пользователя; настоящая защита — сама привязка DPAPI к профилю Windows.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("BIMHubPlugin.TokenStore.v1");

        public static CachedToken Load()
        {
            try
            {
                if (!File.Exists(TokenPath))
                    return null;

                var encrypted = File.ReadAllBytes(TokenPath);
                var decrypted = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decrypted);
                return JsonConvert.DeserializeObject<CachedToken>(json);
            }
            catch (Exception ex)
            {
                // Повреждённый/чужой блоб (например, профиль скопирован на другую машину) —
                // не блокируем работу, просто считаем, что кэша нет, попросим перелогиниться.
                SimpleLogger.Error("TokenStore.Load failed", ex);
                return null;
            }
        }

        public static void Save(CachedToken token)
        {
            try
            {
                var dir = Path.GetDirectoryName(TokenPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonConvert.SerializeObject(token);
                var plain = Encoding.UTF8.GetBytes(json);
                var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(TokenPath, encrypted);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error("TokenStore.Save failed", ex);
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(TokenPath))
                    File.Delete(TokenPath);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error("TokenStore.Clear failed", ex);
            }
        }
    }
}
