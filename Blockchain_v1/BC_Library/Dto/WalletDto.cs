using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BC_Library.Dto
{
    public class WalletDto
    {
        public string PublicKey { get; set; }
        public RSA PrivateKey{ get; set; }
        public WalletDto()
        {
            using var rsa = RSA.Create(2048);
            PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
            PrivateKey = RSA.Create();
            PrivateKey.ImportRSAPrivateKey(rsa.ExportRSAPrivateKey(), out _);
            
        }
        public TransactionDto CreateTransaction(string toAddress, decimal amount)
        {
            var tx = new TransactionDto(PublicKey, toAddress, amount);
            tx.SignTransaction(PrivateKey);
            return tx;
        }
    }
}
