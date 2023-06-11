using ListNodeSerializer;
using SerializerTests;
using SerializerTests.Implementations;
using SerializerTests.Nodes;

namespace Tests;

public class Tests
{
    private RgarothSerializer _serializer;

    [SetUp]
    public void Setup()
    {
        _serializer = new RgarothSerializer();
    }

    [Test]
    public void Deserialize_IncorrectDataInStream_ThrowArgumentException()
    {
        var stream = new MemoryStream();
        stream.Write("123"u8);
        
        var stream2 = new MemoryStream();
        stream2.Write("1.1.1.1"u8);
        
        Assert.ThrowsAsync<ArgumentException>(async () => await _serializer.Deserialize(new MemoryStream()));
        Assert.ThrowsAsync<ArgumentException>(async () => await _serializer.Deserialize(stream));
        Assert.ThrowsAsync<ArgumentException>(async () => await _serializer.Deserialize(stream2));
    }

    [Test]
    public async Task SerializeAndDeserialize_DataEquals_True()
    {
        var node = ListNodeGenerator.Generate(1000000);
        var data = ExtractData(node);
        
        var stream = new MemoryStream();
        await _serializer.Serialize(node, stream);
        var newNode = await _serializer.Deserialize(stream);
        
        Assert.True(IsEqualsData(newNode, data));
    }
    
    [Test]
    public async Task ByteSerializeAndDeserialize_DataEquals_True()
    {
        var node = ListNodeGenerator.Generate(10000);
        var data = ExtractData(node);
        
        var stream = new MemoryStream();
        await _serializer.ByteSerialize(node, stream);
        var newNode = await _serializer.ByteDeserialize(stream);
        
        Assert.True(IsEqualsData(newNode, data));
    }

    [Test]
    public async Task DeepCopy_DataEquals_True()
    {
        var node = ListNodeGenerator.Generate(100);
        var data = ExtractData(node);

        var newNode = await _serializer.DeepCopy(node);

        Assert.True(IsEqualsData(newNode, data));
    }

    private static bool IsEqualsData(ListNode newNode, IReadOnlyList<(string Data, string? RandomData)> data)
    {
        var i = 0;
        
        while (newNode != null)
        {
            if (!string.Equals(newNode.Data, data[i].Data) ||
                !string.Equals(newNode.Random?.Data, data[i].RandomData))
            {
                return false;
            }
            newNode = newNode.Next;

            i++;
        }

        return true;
    }

    private static List<(string, string?)> ExtractData(ListNode node)
    {
        var data = new List<(string Data, string? RandomData)>();

        while (node != null)
        {
            data.Add((node.Data, node.Random?.Data));
            node = node.Next;
        }

        return data;
    }
}