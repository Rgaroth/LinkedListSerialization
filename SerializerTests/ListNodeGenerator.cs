using SerializerTests.Nodes;

namespace SerializerTests;

public static class ListNodeGenerator
{
    public static ListNode Generate(int count)
    {
        var dict = new Dictionary<int, ListNode>();
        
        var firstNode = new ListNode
        {
            Data = "0,.0"
        };
        dict.Add(0, firstNode);

        var prevNode = firstNode;
        
        for (var i = 1; i < count; i++)
        {
            var node = new ListNode
            {
                Previous = prevNode,
                Data = $"{i},.{i}"
            };

            prevNode.Next = node;
            
            prevNode = node;

            dict.Add(i, node);
        }

        var randomsCount = Random.Shared.Next(count / 2, count - 1);
        
        for (var i = 0; i < randomsCount; i++)
        {
            var indexFirst = Random.Shared.Next(0, count - 1);
            var indexSecond = Random.Shared.Next(0, count - 1);

            if (indexFirst == indexSecond)
            {
                i--;
                continue;
            }

            var nodeSource = dict[indexFirst];

            var nodeRandom = dict[indexSecond];

            if (nodeRandom.Random == nodeSource)
            {
                i--;
                continue;
            }
            
            nodeSource.Random = nodeRandom;
        }

        return firstNode;
    }
}