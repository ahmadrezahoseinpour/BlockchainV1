using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BC_Library.Dto
{
    public class WalletDto
    {
        public string PublicKey { get; set; }
        public RSA PrivateKey { get; set; }
        public decimal Stake { get; set; }
        public WalletDto(decimal stake = 0)
        {
            using var rsa = RSA.Create(2048);
            PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
            PrivateKey = RSA.Create();
            PrivateKey.ImportRSAPrivateKey(rsa.ExportRSAPrivateKey(), out _);
            Stake = stake;

        }
        public TransactionDto CreateTransaction(string toAddress, decimal amount, decimal fee, SmartContractDto? smartContract = null)
        {
            var tx = new TransactionDto(PublicKey, toAddress, amount, fee, smartContract);
            tx.SignTransaction(PrivateKey);
            return tx;
        }
        public void SaveToFile(string filePath, string password)
        {
            var keyData = new { PublicKey, PrivateKey = Convert.ToBase64String(PrivateKey.ExportRSAPublicKey()), Stake };
            string json = JsonSerializer.Serialize(keyData);
            byte[] encrypted = Encrypt(json, password);
            File.WriteAllBytes(filePath, encrypted);
        }
        public static WalletDto LoadFormFile(string filePath, string password)
        {
            byte[] encrypted = File.ReadAllBytes(filePath);
            string json = Decrypt(encrypted, password);
            var keyData = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            var wallet = new WalletDto()
            {
                PublicKey = keyData["PublicKey"],
                Stake = decimal.Parse(keyData["Stake"])
            };
            wallet.PrivateKey = RSA.Create();
            wallet.PrivateKey.ImportRSAPrivateKey(Convert.FromBase64String(keyData["PrivateKey"]), out _);
            return wallet;
        }

        private static byte[] Encrypt(string data, string password)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            aes.GenerateIV();
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            cs.Write(bytes, 0, bytes.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }
        private static string Decrypt(byte[] data, string password)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            byte[] iv = data.Take(16).ToArray();
            aes.IV = iv;
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(data, 16, data.Length - 16);
            cs.FlushFinalBlock();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
