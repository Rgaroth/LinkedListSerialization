using BenchmarkDotNet.Attributes;
using ListNodeSerializer;
using SerializerTests;
using SerializerTests.Implementations;
using SerializerTests.Nodes;

namespace Benchmark;

public class BenchSerializer
{

    private ListNode[] _nodes;
    private MemoryStream[] _streams;
    private MemoryStream[] _byteStreams;
    

    [Benchmark()]
    // [Arguments(0)]
    // [Arguments(1)]
    // [Arguments(2)]
    [Arguments(3)]
    public async Task Serialize(int i)
    {
        var ser = new RgarothSerializer();
        using var stream = new MemoryStream();
        await ser.Serialize(_nodes[i], stream);
    }

    [Benchmark()]
    // [Arguments(0)]
    // [Arguments(1)]
    // [Arguments(2)]
    [Arguments(3)]
    public async Task Deserialize(int i)
    {
        var ser = new RgarothSerializer();
        await ser.Deserialize(_streams[i]);
    }
    
    [Benchmark]
    // [Arguments(0)]
    // [Arguments(1)]
    // [Arguments(2)]
    [Arguments(3)]
    public async Task DeepCopy(int i)
    {
        var ser = new RgarothSerializer();
        await ser.DeepCopy(_nodes[i]);
    }

    [GlobalSetup]
    public async Task InitSource()
    {
        var ser = new RgarothSerializer();
        
        _nodes = new[]
        {
            ListNodeGenerator.Generate(1000), 
            ListNodeGenerator.Generate(10000),
            ListNodeGenerator.Generate(100000),
            ListNodeGenerator.Generate(1000000),
        };

        var streams = new List<MemoryStream>();

        foreach (var node in _nodes)
        {
            var stream = new MemoryStream();

            await ser.Serialize(node, stream);
            streams.Add(stream);
        }

        _streams = streams.ToArray();
    }
}