using BenchmarkDotNet.Running;
using Benchmark;
using SerializerTests;
using SerializerTests.Implementations;

BenchmarkRunner.Run<BenchSerializer>();