using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Utility for string encryption/decryption
    /// </summary>
    public static class StringEncryption
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        
        /// <summary>
        /// Generate a random encryption key
        /// </summary>
        public static string GenerateKey()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);
            }
        }
        
        /// <summary>
        /// Encrypt a string using AES
        /// </summary>
        public static string Encrypt(string plainText, string base64Key)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;
                
            try
            {
                byte[] key = Convert.FromBase64String(base64Key);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.GenerateIV();
                    
                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var msEncrypt = new MemoryStream())
                    {
                        // Prepend IV to the encrypted data
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
                        
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Encryption failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Decrypt a string using AES
        /// </summary>
        public static string Decrypt(string cipherText, string base64Key)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;
                
            try
            {
                byte[] key = Convert.FromBase64String(base64Key);
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    // Extract IV from the beginning of the cipher text
                    byte[] iv = new byte[BlockSize / 8];
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
            catch (Exception ex)
            {
                throw new Exception($"Decryption failed: {ex.Message}", ex);
            }
        }
    }
}
