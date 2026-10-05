using System;

namespace BIMHubPlugin.Services
{
    public static class ApiEndpoint
    {
        public static string Normalize(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new ArgumentException("Укажите абсолютный URL API без пароля, query и fragment.");
            // Upgrade the previous shipped default before any credentials leave this process.
            if (uri.Scheme == Uri.UriSchemeHttp && uri.Host.Equals("bimhub.kazgor.kz", StringComparison.OrdinalIgnoreCase))
                uri = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.IsDefaultPort ? 443 : uri.Port }.Uri;
            if (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
                throw new ArgumentException("Для сервера требуется HTTPS. HTTP разрешён только для localhost при разработке.");
            return uri.AbsoluteUri.TrimEnd('/');
        }
    }
}
