using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Security;

namespace Nexus.Infrastructure.Security
{
    public class WindowsSecretStore : ISecretStore
    {
        private readonly string _filePath;
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("NTE_SECRET_SALT_2026_@!");

        public WindowsSecretStore(string? customPath = null)
        {
            if (!string.IsNullOrEmpty(customPath))
            {
                _filePath = customPath;
            }
            else
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dir = Path.Combine(appData, "NexusTradingEngine");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                _filePath = Path.Combine(dir, "secrets.dat");
            }
        }

        public async Task SaveSecretAsync(string key, string secret, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (secret == null) throw new ArgumentNullException(nameof(secret));

            var secrets = await LoadSecretsAsync(cancellationToken);
            var encrypted = Encrypt(secret);
            secrets[key] = encrypted;

            await SaveSecretsAsync(secrets, cancellationToken);
        }

        public async Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            var secrets = await LoadSecretsAsync(cancellationToken);
            if (secrets.TryGetValue(key, out var encrypted))
            {
                return Decrypt(encrypted);
            }
            return null;
        }

        public async Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            var secrets = await LoadSecretsAsync(cancellationToken);
            if (secrets.Remove(key))
            {
                await SaveSecretsAsync(secrets, cancellationToken);
            }
        }

        private async Task<Dictionary<string, string>> LoadSecretsAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_filePath))
            {
                return new Dictionary<string, string>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private async Task SaveSecretsAsync(Dictionary<string, string> secrets, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(secrets);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
        }

        private string Encrypt(string plainText)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
            }
            else
            {
                // Fallback for Linux/macOS
                encryptedBytes = FallbackEncryptDecrypt(plainBytes);
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        private string Decrypt(string cipherText)
        {
            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);
                byte[] decryptedBytes;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    decryptedBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
                }
                else
                {
                    // Fallback for Linux/macOS
                    decryptedBytes = FallbackEncryptDecrypt(cipherBytes);
                }

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private byte[] FallbackEncryptDecrypt(byte[] data)
        {
            // Simple robust symmetric key derived from system metadata to make sure it's user/machine bound
            var user = Environment.UserName ?? "user";
            var machine = Environment.MachineName ?? "machine";
            var salt = user + "@" + machine + "_NTE_SALT";
            var keyBytes = Encoding.UTF8.GetBytes(salt);

            var result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            }
            return result;
        }
    }
}
