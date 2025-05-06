using BC_Library.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Transactions;

namespace BC_Library.Service
{
    public class BlockchainService
    {
        public List<BlockDto> Chain { get; private set; }
        public List<TransactionDto> PendingTransactions { get; private set; }
        public int Difficulty { get; set; } = 4;
        public decimal MiningReward { get; set; } = 10m;
        private readonly string _chainFilePath = "blockchain.json";

        public BlockchainService()
        {
            Chain = LoadChainFromFile() ?? new List<BlockDto> { CreateGenesisBlock() };
            PendingTransactions = new List<TransactionDto>();
        }
        private BlockDto CreateGenesisBlock()
        {
            return new BlockDto(0, new List<TransactionDto>(), "0", Difficulty);
        }
        private List<BlockDto> LoadChainFromFile()
        {
            try
            {
                if (File.Exists(_chainFilePath))
                {
                    string json = File.ReadAllText(_chainFilePath);
                    return JsonSerializer.Deserialize<List<BlockDto>>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading chain: {ex.Message}");
            }
            return null;
        }

        private void SaveChainToFile()
        {
            try
            {
                string json = JsonSerializer.Serialize(Chain, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_chainFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving chain: {ex.Message}");
            }
        }

        public BlockDto GetLatestBlock() => Chain.Last();

        public void MinePendingTransactions(string minerPublicKey)
        {
            var block = new BlockDto(Chain.Count, new List<TransactionDto>(PendingTransactions), GetLatestBlock().Hash, Difficulty);
            CommonService.MineBlock(block, Difficulty);
            Chain.Add(block);
            SaveChainToFile();
            PendingTransactions.Clear();
            PendingTransactions.Add(new TransactionDto(null, minerPublicKey, MiningReward));
        }

        public bool CreateTransaction(TransactionDto tx, RSA publicKey)
        {
            if (!IsValidTransaction(tx, publicKey))
                return false;

            PendingTransactions.Add(tx);
            return true;
        }

        private bool IsValidTransaction(TransactionDto tx, RSA publicKey)
        {
            if (tx.FromAddress != null && !tx.VerifySignature(publicKey))
                return false;

            if (tx.Amount <= 0)
                return false;

            if (tx.FromAddress != null && GetBalance(tx.FromAddress) < tx.Amount)
                return false;

            return true;
        }

        public decimal GetBalance(string publicKey)
        {
            decimal balance = 0;
            foreach (var block in Chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.FromAddress == publicKey)
                        balance -= tx.Amount;
                    if (tx.ToAddress == publicKey)
                        balance += tx.Amount;
                }
            }
            return balance;
        }

        public bool IsChainValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                var current = Chain[i];
                var previous = Chain[i - 1];

                if (current.Hash != current.CalculateHash())
                    return false;

                if (current.PreviousHash != previous.Hash)
                    return false;

                foreach (var tx in current.Transactions)
                {
                    using var rsa = RSA.Create();
                    rsa.ImportRSAPublicKey(Convert.FromBase64String(tx.FromAddress ?? tx.ToAddress), out _);
                    if (!tx.VerifySignature(rsa))
                        return false;
                }
            }
            return true;
        }
    }
}