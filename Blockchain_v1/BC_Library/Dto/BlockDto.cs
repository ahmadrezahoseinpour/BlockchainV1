using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BC_Library.Dto
{
    public class BlockDto
    {
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public List<TransactionDto> Transactions { get; set; }
        public string PreviousHash { get; set; }
        public string Hash { get; set; }
        public int Nonce { get; set; }
        public int Difficulty { get; set; }

        public BlockDto(int index, List<TransactionDto> transactions, string previousHash, int difficulty)
        {
            Index = index;
            Timestamp = DateTime.UtcNow;
            Transactions = transactions;
            PreviousHash = previousHash;
            Nonce = 0;
            Difficulty = difficulty;
            Hash = CalculateHash();
        }
        public string CalculateHash()
        {
            string txData = JsonSerializer.Serialize(Transactions);
            string rawData = Index + Timestamp.ToString("o") + txData + PreviousHash + Nonce + Difficulty;
            using var sha265 = SHA256.Create();
            var bytes = sha265.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToBase64String(bytes);
        }
    }
}
