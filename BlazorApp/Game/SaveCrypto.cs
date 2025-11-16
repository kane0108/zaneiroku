using Microsoft.JSInterop;
using System.Text.Json;
using System.Text;

namespace BlazorApp.Game
{
    /// <summary>
    /// ブラウザWebCrypto APIを使ったAES暗号化/復号（WASM互換）
    /// </summary>
    public static class SaveCrypto
    {
        private const string AES_KEY = "FuumaZaneiRoku_2025_AES256_Key_!!";

        private static IJSRuntime? _js;
        public static void Initialize(IJSRuntime js) => _js = js;

        public static async Task<string> EncryptAndSignAsync(string plain)
        {
            if (_js == null) throw new InvalidOperationException("JSRuntime not initialized");
            string encrypted = await _js.InvokeAsync<string>("fzCrypto.encrypt", plain, AES_KEY);
            string signature = ComputeHmac(encrypted);
            return $"{encrypted}.{signature}";
        }

        public static async Task<string> DecryptAndVerifyAsync(string payload)
        {
            if (_js == null) throw new InvalidOperationException("JSRuntime not initialized");

            var parts = payload.Split('.');
            if (parts.Length != 2)
                throw new InvalidDataException("Invalid payload");

            string encrypted = parts[0];
            string signature = parts[1];
            if (!SlowEquals(signature, ComputeHmac(encrypted)))
                throw new InvalidDataException("Signature mismatch");

            string plain = await _js.InvokeAsync<string>("fzCrypto.decrypt", encrypted, AES_KEY);
            return plain;
        }

        // === 以下はC#側でHMAC署名 ===
        private static string ComputeHmac(string text)
        {
            using var h = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes("FZ_HmacKey_2025_Signature_Protect"));
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(h.ComputeHash(bytes));
        }

        private static bool SlowEquals(string a, string b)
        {
            var ba = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);
            int diff = ba.Length ^ bb.Length;
            for (int i = 0; i < Math.Min(ba.Length, bb.Length); i++)
                diff |= ba[i] ^ bb[i];
            return diff == 0;
        }
    }
}
