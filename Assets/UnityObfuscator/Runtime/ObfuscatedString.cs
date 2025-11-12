using System;
using System.IO;
using System.Security.Cryptography;

namespace UnityObfuscator.Runtime
{
    /// <summary>
    /// Runtime utility for decrypting obfuscated strings
    /// This class will be injected into obfuscated assemblies
    /// </summary>
    public static class ObfuscatedString
    {
        private static string _key;
        
        /// <summary>
        /// Initialize the decryption key (called once during startup)
        /// </summary>
        public static void Initialize(string key)
        {
            _key = key;
        }
        
        /// <summary>
        /// Decrypt an obfuscated string at runtime
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;
                
            if (string.IsNullOrEmpty(_key))
            {
                throw new InvalidOperationException("ObfuscatedString not initialized. Call Initialize() first.");
            }
            
            try
            {
                byte[] key = Convert.FromBase64String(_key);
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    // Extract IV from the beginning
                    byte[] iv = new byte[16]; // 128 bits
                    Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                    aes.IV = iv;
                    
                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var msDecrypt = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch
            {
                // Return original if decryption fails (in case it's not encrypted)
                return cipherText;
            }
        }
    }
}
