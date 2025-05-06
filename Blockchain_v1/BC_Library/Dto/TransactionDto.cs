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
        public string  Signature { get; set; }
        public DateTime Timestamp { get; set; }

        public TransactionDto(string from, string to, decimal amount)
        {
            FromAddress = from;
            ToAddress = to;
            Amount = amount;
            Timestamp = DateTime.Now;
        }

        public string CalculateHash()
        {
            string data = FromAddress + ToAddress + Amount.ToString() + Timestamp.ToString("o");
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
            if(Signature == null) return FromAddress == null;
            var hash = CalculateHash();
            var signatureByte = Convert.FromBase64String(Signature);
            return publicKey.VerifyData(Encoding.UTF8.GetBytes(hash), signatureByte, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }

}
