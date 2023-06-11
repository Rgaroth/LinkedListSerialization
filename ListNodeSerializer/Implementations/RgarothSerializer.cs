using System.Text;
using ListNodeSerializer.Nodes;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;
using SerializerTests.Interfaces;
using SerializerTests.Nodes;

namespace SerializerTests.Implementations;

public class RgarothSerializer : IListSerializer
{
    public async Task Serialize(ListNode head, Stream stream)
    {
        var tempNodes = GetNodesInfo(head, true);

        foreach (var node in tempNodes)
        {
            await stream.WriteAsync(BitConverter.GetBytes(node.Id));
            await stream.WriteAsync(BitConverter.GetBytes(node.RandId ?? -1));
            await stream.WriteAsync(BitConverter.GetBytes(node.Data.Length));
            await stream.WriteAsync(Encoding.UTF8.GetBytes(node.Data));
        }

        //return Task.CompletedTask;
    }

    public async Task<ListNode> Deserialize(Stream stream)
    {
        if (stream.Length < sizeof(int) * 3)
        {
            throw new ArgumentException();
        }
        
        var nodes = new Dictionary<long, (ListNode ListNode, NodeInfo Info)>();

        ListNode prev = null;
        stream.Position = 0;

        while (stream.Position < stream.Length)
        {
            var bytes = new byte[4];

            var readBytesId = await stream.ReadAsync(bytes, 0, sizeof(int));
            var nodeId = BitConverter.ToInt32(bytes);
            
            var readBytesRandId = await stream.ReadAsync(bytes, 0, sizeof(int));
            var nodeRandomId = BitConverter.ToInt32(bytes);
            
            var readBytesDataSize = await stream.ReadAsync(bytes, 0, sizeof(int));
            var dataLength = BitConverter.ToInt32(bytes);

            if (readBytesId != 4 ||
                readBytesRandId != 4 ||
                readBytesDataSize != 4 ||
                nodeId < 0 ||
                nodeRandomId < -1 ||
                dataLength < 0 )
            {
                throw new ArgumentException();
            }

            var dataBytes = new byte[dataLength];
            var dataRealRead = await stream.ReadAsync(dataBytes, 0, dataBytes.Length);

            var nodeData = Encoding.UTF8.GetString(dataBytes);

            if (dataRealRead != dataLength)
            {
                throw new ArgumentException();
            }
            
            var nodeInfo = new NodeInfo
            {
                Id = nodeId,
                RandId = nodeRandomId == -1 ? null : nodeRandomId
            };

            var listNode = new ListNode
            {
                Previous = prev,
                Data = ReplaceSpecialSymbols(nodeData, false)
            };

            if (prev != null) prev.Next = listNode;

            prev = listNode;

            nodes.Add(nodeInfo.Id, (listNode, nodeInfo));
        }

        if (!nodes.Any()) throw new ArgumentException();

        try
        {
            RestoreRandomNodes(nodes);
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentException();
        }

        return nodes.First().Value.ListNode;
    }

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
                Previous = prevNewNode
            };

            if (prevNewNode != null) prevNewNode.Next = newNode;

            newNodes.Add(nodeInfo.Id, (newNode, nodeInfo));

            prevNewNode = newNode;
        }

        RestoreRandomNodes(newNodes);

        return Task.FromResult(newNodes.First().Value.ListNode);
    }

    private static void RestoreRandomNodes(IReadOnlyDictionary<long, (ListNode ListNode, NodeInfo Info)> nodes)
    {
        foreach (var node in nodes.Where(node => node.Value.Info.RandId.HasValue))
            node.Value.ListNode.Random = nodes[node.Value.Info.RandId.Value].ListNode;
    }
    
    private IEnumerable<NodeInfo> GetNodesInfo(ListNode head, bool isShield)
    {
        var id = 0;
        var order = 0;
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