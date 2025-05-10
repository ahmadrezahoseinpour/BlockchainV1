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
        private readonly List<BlockDto> _chain;
        private readonly ConcurrentBag<TransactionDto> _pendingTransactions;
        private readonly Dictionary<string, decimal> _balanceCache;
        private readonly List<WalletDto> _validators;
        private readonly string _chainFilePath;
        private readonly ILogger<BlockchainService> _logger;
        private readonly object _fileLock = new object();
        private readonly Random _random = new Random();

        // Constants
        private const decimal MINIMUM_FEE = 0.1m;
        private const decimal VALIDATOR_REWARD = 5m;
        private const decimal MINIMUM_STAKE = 100m;

        public BlockchainService(ILogger<BlockchainService> logger, string chainFilePath = "blockchain.json")
        {
            _logger = logger;
            _chainFilePath = chainFilePath;
            _chain = LoadChainFromFile() ?? new List<BlockDto> { CreateGenesisBlock() };
            _pendingTransactions = new ConcurrentBag<TransactionDto>();
            _validators = new List<WalletDto>();
            _balanceCache = new Dictionary<string, decimal>();
        }
        
        private BlockDto CreateGenesisBlock()
        {
            return new BlockDto(0, new List<TransactionDto>(), "0", string.Empty, DateTime.UtcNow);
        }

        private List<BlockDto> LoadChainFromFile()
        {
            try
            {
                lock (_fileLock)
                {
                    if (File.Exists(_chainFilePath))
                    {
                        string json = File.ReadAllText(_chainFilePath);
                        return JsonSerializer.Deserialize<List<BlockDto>>(json);
                    }
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
                lock (_fileLock)
                {
                    string json = JsonSerializer.Serialize(_chain, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_chainFilePath, json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving blockchain to file");
            }
        }

        public void RegisterValidator(WalletDto wallet)
        {
            if (wallet.Stake < MINIMUM_STAKE)
            {
                _logger.LogWarning($"Validator registration failed: Insufficient stake ({wallet.Stake})");
                return;
            }

            lock (_validators)
            {
                _validators.Add(wallet);
                _logger.LogInformation($"Validator registered: {wallet.PublicKey.Substring(0, 20)}...");
            }
        }
        
        public bool CreateTransaction(TransactionDto tx, RSA publicKey)
        {
            if (!IsValidTransaction(tx, publicKey))
            {
                _logger.LogWarning("Invalid transaction rejected");
                return false;
            }

            _pendingTransactions.Add(tx);
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

            if (tx.Amount <= 0 || tx.Fee < MINIMUM_FEE)
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

            // Check for double-spending
            if (_pendingTransactions.Any(pt => pt.FromAddress == tx.FromAddress && pt.Id == tx.Id))
            {
                _logger.LogWarning("Double-spending attempt detected");
                return false;
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

            var transactions = _pendingTransactions.ToList();
            var block = new BlockDto(_chain.Count, transactions, _chain.Last().Hash, validatorPublicKey, DateTime.UtcNow);
            lock (_chain)
            {
                _chain.Add(block);
            }

            SaveChainToFile();
            _pendingTransactions.Clear();
            _pendingTransactions.Add(new TransactionDto(null, validatorPublicKey, VALIDATOR_REWARD, 0m));
            _logger.LogInformation($"Block #{block.Index} validated by {validatorPublicKey.Substring(0, 20)}...");

            // Update balance cache
            UpdateBalanceCache(transactions);
        }

        public WalletDto SelectValidator()
        {
            lock (_validators)
            {
                if (_validators.Count == 0) return null;

                // Weighted random selection based on stake
                decimal totalStake = _validators.Sum(v => v.Stake);
                decimal randomValue = (decimal)_random.NextDouble() * totalStake;
                decimal cumulative = 0;

                foreach (var validator in _validators)
                {
                    cumulative += validator.Stake;
                    if (randomValue <= cumulative)
                    {
                        return validator;
                    }
                }

                return _validators.Last(); // Fallback
            }
        }

        public decimal GetBalance(string publicKey)
        {
            if (_balanceCache.TryGetValue(publicKey, out decimal cachedBalance))
            {
                return cachedBalance;
            }

            decimal balance = 0;
            foreach (var block in _chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.FromAddress == publicKey)
                        balance -= (tx.Amount + tx.Fee);
                    if (tx.ToAddress == publicKey)
                        balance += tx.Amount;
                }
            }

            _balanceCache[publicKey] = balance;
            return balance;
        }

        private void UpdateBalanceCache(List<TransactionDto> transactions)
        {
            foreach (var tx in transactions)
            {
                if (tx.FromAddress != null)
                {
                    _balanceCache[tx.FromAddress] = GetBalance(tx.FromAddress);
                }
                if (tx.ToAddress != null)
                {
                    _balanceCache[tx.ToAddress] = GetBalance(tx.ToAddress);
                }
            }
        }

        public bool IsChainValid()
        {
            for (int i = 1; i < _chain.Count; i++)
            {
                var current = _chain[i];
                var previous = _chain[i - 1];

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
