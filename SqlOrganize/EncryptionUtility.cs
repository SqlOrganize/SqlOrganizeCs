using System.Security.Cryptography;
using System.Text;

namespace SqlOrganize
{
    public class EncryptionUtility
    {
        public static string Key; // 16 bytes for AES-128

        public static string Encrypt(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(Key);
                aesAlg.GenerateIV();

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    byte[] iv = aesAlg.IV;
                    byte[] encryptedBytes = msEncrypt.ToArray();
                    byte[] resultBytes = new byte[iv.Length + encryptedBytes.Length];
                    Buffer.BlockCopy(iv, 0, resultBytes, 0, iv.Length);
                    Buffer.BlockCopy(encryptedBytes, 0, resultBytes, iv.Length, encryptedBytes.Length);
                    return Convert.ToBase64String(resultBytes);
                }
            }
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] allBytes = Convert.FromBase64String(encryptedText);
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(Key);

                byte[] iv = new byte[aesAlg.BlockSize / 8];
                byte[] cipherText = new byte[allBytes.Length - iv.Length];
                Buffer.BlockCopy(allBytes, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(allBytes, iv.Length, cipherText, 0, cipherText.Length);

                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}