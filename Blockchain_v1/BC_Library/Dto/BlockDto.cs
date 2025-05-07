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
        public string MerkleRoot { get; set; }
        public string Validator { get; set; } // Public key of the validator (PoS)

        public BlockDto(int index, List<TransactionDto> transactions, string previousHash, string validator)
        {
            Index = index;
            Timestamp = DateTime.UtcNow;
            Transactions = transactions;
            PreviousHash = previousHash;
            Validator = validator;
            MerkleRoot = CalculateMerkleRoot();
            Hash = CalculateHash();
        }

        public string CalculateHash()
        {
            string txData = JsonSerializer.Serialize(Transactions);
            string rawData = Index + Timestamp.ToString("o") + txData + PreviousHash + MerkleRoot + Validator;
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToBase64String(bytes);
        }

        public string CalculateMerkleRoot()
        {
            if (Transactions.Count == 0) return string.Empty;
            var hashes = Transactions.ConvertAll(tx => tx.CalculateHash());
            while (hashes.Count > 1)
            {
                var temp = new List<string>();
                for (int i = 0; i < hashes.Count; i += 2)
                {
                    string combined = i + 1 < hashes.Count ? hashes[i] + hashes[i + 1] : hashes[i];
                    using var sha256 = SHA256.Create();
                    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    temp.Add(Convert.ToBase64String(bytes));
                }
                hashes = temp;
            }
            return hashes[0];
        }
    }
}

