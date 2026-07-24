using System.Collections.Generic;

namespace BIMHubPlugin.Models
{
    /// <summary>Ответ POST /api/auth/login BimHelpDesk.</summary>
    public class LoginResult
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
