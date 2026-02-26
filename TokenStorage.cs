using System;

namespace autoquest
{
    public static class TokenStorage
    {
        public static void SaveToken(string token, DateTime expiry, string fingerprint) { }
        public static TokenData? LoadToken() => null;
    }

    public class TokenData
    {
        public string Token { get; set; } = "";
        public DateTime Expiry { get; set; }
        public string Fingerprint { get; set; } = "";
    }
}
