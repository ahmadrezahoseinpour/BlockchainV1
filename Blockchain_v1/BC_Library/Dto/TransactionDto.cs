using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Transactions;

namespace BC_Library.Dto
{
    public class TransactionDto
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string  Signature { get; set; }
        public DateTime Timestamp { get; set; }
        public SmartContractDto? SmartContract { get; set; }

        public TransactionDto(string from, string to, decimal amount, decimal fee, SmartContractDto? smartContract = null)
        {
            FromAddress = from;
            ToAddress = to;
            Amount = amount;
            Fee = fee;
            Timestamp = DateTime.UtcNow;
            SmartContract = smartContract;
        }

        public string CalculateHash()
        {
            string contractData = SmartContract != null ? JsonSerializer.Serialize(SmartContract) : string.Empty;
            string data = FromAddress + ToAddress + Amount.ToString() + Fee.ToString() + Timestamp.ToString("o") + contractData;
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }
        public void SignTransaction(RSA privateKey)
        {
            if (FromAddress == null) return; 
            string hash = CalculateHash();
            var signatureBytes = privateKey.SignData(Encoding.UTF8.GetBytes(hash), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Signature = Convert.ToBase64String(signatureBytes);
        }

        public bool VerifySignature(RSA publicKey)
        {
            if (Signature == null) return FromAddress == null;
            var hash = CalculateHash();
            var signatureBytes = Convert.FromBase64String(Signature);
            return publicKey.VerifyData(Encoding.UTF8.GetBytes(hash), signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }

}
