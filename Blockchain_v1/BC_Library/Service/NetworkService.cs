using BC_Library.Dto;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BC_Library.Service
{
    public class NetworkService
    {
        private readonly List<BlockchainService> _nodes;
        private readonly ILogger<NetworkService> _logger;

        public NetworkService(ILogger<NetworkService> logger)
        {
            _nodes = new List<BlockchainService>();
            _logger = logger;
        }

        public void AddNode(BlockchainService node)
        {
            _nodes.Add(node);
            _logger.LogInformation("Node added to network");
            SynchronizeNodes();
        }

        public void BroadcastTransaction(TransactionDto tx, RSA publicKey)
        {
            foreach (var node in _nodes)
            {
                if (node.CreateTransaction(tx, publicKey))
                {
                    _logger.LogInformation($"Transaction broadcasted to node");
                }
            }
        }

        public void BroadcastBlock(BlockDto block)
        {
            foreach (var node in _nodes)
            {
                if (node.Chain.Last().Hash != block.PreviousHash) continue;
                node.Chain.Add(block);
                node.SaveChainToFile();
                _logger.LogInformation($"Block #{block.Index} broadcasted to node");
            }
        }

        private void SynchronizeNodes()
        {
            var longestChain = _nodes.MaxBy(n => n.Chain.Count)?.Chain;
            if (longestChain == null) return;

            foreach (var node in _nodes)
            {
                if (node.Chain.Count < longestChain.Count && node.IsChainValid())
                {
                    node.Chain = new List<BlockDto>(longestChain);
                    node.SaveChainToFile();
                    _logger.LogInformation("Node synchronized with longest chain");
                }
            }
        }
    }
}
