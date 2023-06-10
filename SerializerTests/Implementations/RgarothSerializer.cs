using System.Text;
using System.Text.RegularExpressions;
using SerializerTests.Nodes;
using NodeInfo = SerializerTests.Nodes.NodeInfo;

namespace SerializerTests.Implementations;

public class RgarothSerializer
{
    public Task<ListNode> DeepCopy(ListNode head)
    {
        var nodesInfo = GetNodesInfo(head, false);

        var newNodes = new Dictionary<long, (ListNode ListNode, NodeInfo Info)>();
        ListNode prevNewNode = null;

        foreach (var nodeInfo in nodesInfo)
        {
            var newNode = new ListNode
            {
                Data = nodeInfo.Data,
                Previous = prevNewNode,
            };

            if (prevNewNode != null)
            {
                prevNewNode.Next = newNode;
            }

            newNodes.Add(nodeInfo.Id, (newNode, nodeInfo));

            prevNewNode = newNode;
        }

        RestoreRandomNodes(newNodes);

        return Task.FromResult(newNodes.First().Value.ListNode);
    }

    public async Task<ListNode> Deserialize(Stream stream)
    {
        stream.Position = 0;
        var nodes = new Dictionary<long, (ListNode ListNode, NodeInfo Info)>();
        var reader = new StreamReader(stream, Encoding.UTF8);

        var tail = string.Empty;
        
        while (!reader.EndOfStream)
        {
            var text = await reader.ReadLineAsync();
            text = tail + text;
            
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException();
            }
            
            var nodesSerial = Regex.Split(text, @"(?<!\\),").ToList();
            var curTail = nodesSerial.Last();
            nodesSerial.RemoveAt(nodesSerial.Count - 1);
            
            tail = string.IsNullOrEmpty(curTail) ? string.Empty : curTail;

            ListNode prev = null;

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
        }

        if (!nodes.Any())
        {
            throw new ArgumentException();
        }

        RestoreRandomNodes(nodes);

        return nodes.First().Value.ListNode;
    }

    private static void RestoreRandomNodes(IReadOnlyDictionary<long, (ListNode ListNode, NodeInfo Info)> nodes)
    {
        foreach (var node in nodes.Where(node => node.Value.Info.RandId.HasValue))
        {
            node.Value.ListNode.Random = nodes[node.Value.Info.RandId.Value].ListNode;
        }
    }

    public async Task Serialize(ListNode head, Stream stream)
    {
        var tempNodes = GetNodesInfo(head, true);

        var serial = string.Join(',',
            tempNodes
                .Select(item => $"{item.Id}.{item.RandId}.{item.Data}"));

        var bytes = Encoding.UTF8.GetBytes(serial);

        await stream.WriteAsync(bytes);
    }

    private IEnumerable<NodeInfo> GetNodesInfo(ListNode head, bool isShield)
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

        return tempNodes
            .Select(x => x.Value)
            .OrderBy(x => x.Order);
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
}
