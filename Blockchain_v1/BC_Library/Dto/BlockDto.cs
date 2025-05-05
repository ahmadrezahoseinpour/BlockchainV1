using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BC_Library.Dto
{
    public class BlockDto
    {
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string Data { get; set; }
        public string PreviousHash { get; set; }
        public string Hash { get; set; }
        public int Nonce { get; set; }

        public BlockDto(int index, string data, string previousHash)
        {
            Index = index;
            Timestamp = DateTime.UtcNow;
            Data = data;
            PreviousHash = previousHash;
            Nonce = 0;
            Hash = CalculateHash();
        }
        public string CalculateHash()
        {
            string rawData = Index + Timestamp.ToString() + Data + PreviousHash + Nonce;
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return Convert.ToBase64String(bytes);
            }
        }

    }
}
