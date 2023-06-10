﻿using BenchmarkDotNet.Attributes;
using SerializerTests;
using SerializerTests.Implementations;
using SerializerTests.Nodes;

namespace ConsoleApp1;

public class BenchSerializer
{

    private ListNode[] _nodes;
    
    [Benchmark]
    [Arguments(3)]
    public async Task Serialize(int i)
    {
        var ser = new RgarothSerializer();
        using var stream = new MemoryStream();
        await ser.Serialize(_nodes[i], stream);
    }

    // [Benchmark]
    // [Arguments(0)]
    // [Arguments(1)]
    // [Arguments(2)]
    public async Task Deserialize(int i)
    {
        
    }
    
    // [Benchmark]
    // [Arguments(0)]
    // [Arguments(1)]
    // [Arguments(2)]
    public async Task DeepCopy(int i)
    {
        var ser = new RgarothSerializer();
        await ser.DeepCopy(_nodes[i]);
    }

    [GlobalSetup]
    public async Task InitSource()
    {
        _nodes = new[]
        {
            ListNodeGenerator.Generate(1000), 
            ListNodeGenerator.Generate(10000),
            ListNodeGenerator.Generate(100000),
            ListNodeGenerator.Generate(1000000),
        };
    }
}