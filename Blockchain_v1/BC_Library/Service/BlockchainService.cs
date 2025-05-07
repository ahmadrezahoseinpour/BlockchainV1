using BC_Library.Dto;
using Microsoft.Extensions.Logging;
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
        public List<BlockDto> Chain { get; set; }
        public List<TransactionDto> PendingTransactions { get; private set; }
        public decimal MinimumFee { get; set; } = 0.1m;
        public decimal ValidatorReward { get; set; } = 5m;
        private readonly string _chainFilePath = "blockchain.json";
        private readonly ILogger<BlockchainService> _logger;
        private readonly List<WalletDto> _validators;

        public BlockchainService(ILogger<BlockchainService> logger)
        {
            _logger = logger;
            Chain = LoadChainFromFile() ?? new List<BlockDto> { CreateGenesisBlock() };
            PendingTransactions = new List<TransactionDto>();
            _validators = new List<WalletDto>();
        }
        private BlockDto CreateGenesisBlock()
        {
            return new BlockDto(0, new List<TransactionDto>(), "0", string.Empty);
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
                _logger.LogError(ex, "Error loading blockchain from file");
            }
            return null;
        }

        public void SaveChainToFile()
        {
            try
            {
                string json = JsonSerializer.Serialize(Chain, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_chainFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving blockchain to file");
            }
        }
        public void RegisterValidator(WalletDto wallet)
        {
            if (wallet.Stake >= 100m) // Minimum stake requirement
            {
                _validators.Add(wallet);
                _logger.LogInformation($"Validator registered: {wallet.PublicKey.Substring(0, 20)}...");
            }
            else
            {
                _logger.LogWarning($"Validator registration failed: Insufficient stake ({wallet.Stake})");
            }
        }

        public bool CreateTransaction(TransactionDto tx, RSA publicKey)
        {
            if (!IsValidTransaction(tx, publicKey))
            {
                _logger.LogWarning("Invalid transaction rejected");
                return false;
            }
            PendingTransactions.Add(tx);
            _logger.LogInformation($"Transaction added: {tx.Amount} from {tx.FromAddress?.Substring(0, 20)}...");
            return true;
        }

        private bool IsValidTransaction(TransactionDto tx, RSA publicKey)
        {
            if (tx.FromAddress != null && !tx.VerifySignature(publicKey))
            {
                _logger.LogWarning("Transaction signature verification failed");
                return false;
            }

            if (tx.Amount <= 0 || tx.Fee < MinimumFee)
            {
                _logger.LogWarning($"Invalid transaction: Amount={tx.Amount}, Fee={tx.Fee}");
                return false;
            }

            if (tx.FromAddress != null)
            {
                decimal balance = GetBalance(tx.FromAddress);
                if (balance < tx.Amount + tx.Fee)
                {
                    _logger.LogWarning($"Insufficient balance: {balance} < {tx.Amount + tx.Fee}");
                    return false;
                }

                if (tx.SmartContract != null && !tx.SmartContract.Execute(balance, tx.Amount))
                {
                    _logger.LogWarning("Smart contract execution failed");
                    return false;
                }
            }

            return true;
        }

        public void ValidatePendingTransactions(string validatorPublicKey)
        {
            if (!_validators.Any(v => v.PublicKey == validatorPublicKey))
            {
                _logger.LogWarning($"Unauthorized validator: {validatorPublicKey.Substring(0, 20)}...");
                return;
            }

            var block = new BlockDto(Chain.Count, new List<TransactionDto>(PendingTransactions), Chain.Last().Hash, validatorPublicKey);
            Chain.Add(block);
            SaveChainToFile();
            PendingTransactions.Clear();
            PendingTransactions.Add(new TransactionDto(null, validatorPublicKey, ValidatorReward, 0m));
            _logger.LogInformation($"Block #{block.Index} validated by {validatorPublicKey.Substring(0, 20)}...");
        }

        public WalletDto SelectValidator()
        {
            if (_validators.Count == 0) return null;
            // Simplified PoS: Select validator with highest stake
            return _validators.OrderByDescending(v => v.Stake).First();
        }

        public decimal GetBalance(string publicKey)
        {
            decimal balance = 0;
            foreach (var block in Chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.FromAddress == publicKey)
                        balance -= (tx.Amount + tx.Fee);
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
                {
                    _logger.LogError($"Invalid hash in block #{current.Index}");
                    return false;
                }

                if (current.PreviousHash != previous.Hash)
                {
                    _logger.LogError($"Invalid previous hash in block #{current.Index}");
                    return false;
                }

                if (current.MerkleRoot != current.CalculateMerkleRoot())
                {
                    _logger.LogError($"Invalid Merkle root in block #{current.Index}");
                    return false;
                }

                foreach (var tx in current.Transactions)
                {
                    using var rsa = RSA.Create();
                    rsa.ImportRSAPublicKey(Convert.FromBase64String(tx.FromAddress ?? tx.ToAddress), out _);
                    if (!tx.VerifySignature(rsa))
                    {
                        _logger.LogError($"Invalid transaction signature in block #{current.Index}");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}