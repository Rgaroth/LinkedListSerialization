using System.Text;
using System.Text.RegularExpressions;
using SerializerTests.Interfaces;
using SerializerTests.Nodes;

namespace SerializerTests.Implementations
{
    public class RgarothSerializer : IListSerializer
    {
        public Task<ListNode> DeepCopy(ListNode head)
        {
            var nodes = GetNodesInfo(head, false)
                .OrderBy(x => x.Value.Order);
            
            var newNodes = new Dictionary<long, (ListNode ListNode, NodeInfo Info)>();
            ListNode prevNewNode = null;

            foreach (var node in nodes)
            {
                var newNode = new ListNode
                {
                    Data = node.Key.Data,
                    Previous = prevNewNode,
                };

                if (prevNewNode != null)
                {
                    prevNewNode.Next = newNode;
                }
                
                newNodes.Add(node.Value.Id, (newNode, node.Value));

                prevNewNode = newNode;
            }

            RestoreRandomNodes(newNodes);

            return Task.FromResult(newNodes.First().Value.ListNode);
        }

        public async Task<ListNode> Deserialize(Stream stream)
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var text = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException();
            }
            
            var nodesSerial = Regex.Split(text, @"(?<!\\),").ToList();

            ListNode prev = null;

            var nodes = new Dictionary<long, (ListNode ListNode, NodeInfo Info)>();
            
            var splitedNodes = nodesSerial
                .Select(str => Regex.Split(str, @"(?<!\\)\."))
                .ToList();

            if (!splitedNodes.Any())
            {
                throw new ArgumentException();
            }

            foreach (var parts in splitedNodes)
            {
                if (parts.Length != 3)
                {
                    throw new ArgumentException();
                }
                
                var nodeInfo = new NodeInfo
                {
                    Id = int.TryParse(parts[0], out var id) 
                        ? id 
                        : throw new ArgumentException(),
                    
                    RandId = int.TryParse(parts[1], out var randId) 
                        ? randId 
                        : string.IsNullOrEmpty(parts[1]) 
                            ? null 
                            : throw new ArgumentException(),
                };

                var listNode = new ListNode
                {
                    Previous = prev,
                    Data = ReplaceSpecialSymbols(parts[2], false)
                };

                if (prev != null)
                {
                    prev.Next = listNode;
                }

                prev = listNode;

                nodes.Add(nodeInfo.Id, (listNode, nodeInfo));
            }

            RestoreRandomNodes(nodes);

            return nodes.First().Value.ListNode;
        }

        public async Task Serialize(ListNode head, Stream stream)
        {
            var tempNodes = GetNodesInfo(head, true);

            var serial = string.Join(',',
                tempNodes
                    .OrderBy(x => x.Value.Order)
                    .Select(item => $"{item.Value.Id}.{item.Value.RandId}.{item.Value.Data}"));

            var bytes = Encoding.UTF8.GetBytes(serial);

            await stream.WriteAsync(bytes);
        }

        private Dictionary<ListNode, NodeInfo> GetNodesInfo(ListNode head, bool isShield)
        {
            long id = 0;
            long order = 0;
            var tempNodes = new Dictionary<ListNode, NodeInfo>();

            var currentNode = head;

            while (currentNode != null)
            {
                var existNode = tempNodes.TryGetValue(currentNode, out var exist) ? exist : null;

                // если уже содержит текущую ноду, значит мы прошли по этой ноде и всем ее Random
                // назначаем ей текущий номер по порядку и пропускаем
                if (existNode != null)
                {
                    existNode.Order = order++;
                    currentNode = currentNode.Next;

                    continue;
                }

                var node = new NodeInfo
                {
                    Id = id++,
                    Order = order++,
                    Data = isShield 
                        ? ReplaceSpecialSymbols(currentNode.Data, true) 
                        : currentNode.Data
                };

                tempNodes.Add(currentNode, node);

                var currentRand = currentNode.Random;
                var prevRand = node;

                // идем по всем Random до тех пор, пока следующая нода не вернет null
                while (currentRand != null)
                {
                    var existRandNode = tempNodes.TryGetValue(currentRand, out var existRand) ? existRand : null;

                    if (existRandNode != null)
                    {
                        prevRand.RandId = existRandNode.Id;
                        break;
                    }

                    var randNode = new NodeInfo
                    {
                        Id = id++,
                        Data = isShield 
                            ? ReplaceSpecialSymbols(currentRand.Data, true)
                            : currentRand.Data
                    };

                    prevRand.RandId = randNode.Id;

                    tempNodes.Add(currentRand, randNode);
                    prevRand = randNode;
                    currentRand = currentRand.Random;
                }

                currentNode = currentNode.Next;
            }

            return tempNodes;
        }

        private string ReplaceSpecialSymbols(string source, bool shield)
        {
            return shield
                ? source
                    .Replace(".", "\\.")
                    .Replace(",", "\\,")
                : source
                    .Replace("\\.", ".")
                    .Replace("\\,", ",");
        }

        private static void RestoreRandomNodes(IReadOnlyDictionary<long, (ListNode ListNode, NodeInfo Info)> newNodes)
        {
            foreach (var node in newNodes.Where(node => node.Value.Info.RandId.HasValue))
            {
                node.Value.ListNode.Random = newNodes[node.Value.Info.RandId.Value].ListNode;
            }
        }
    }
}