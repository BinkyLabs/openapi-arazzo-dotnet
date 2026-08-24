```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat) (container)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Gen0    | Gen1   | Allocated |
|--------------- |----------:|-----------:|----------:|--------:|-------:|----------:|
| FormalBnplYaml | 537.20 μs | 871.472 μs | 47.768 μs | 25.3906 | 5.8594 | 427.97 KB |
| FormalBnplJson | 188.98 μs | 265.366 μs | 14.546 μs | 14.6484 | 0.9766 |  255.2 KB |
| FapiParYaml    | 399.70 μs | 407.801 μs | 22.353 μs | 21.4844 | 3.9063 | 372.76 KB |
| FapiParJson    | 173.85 μs | 251.258 μs | 13.772 μs | 14.6484 | 2.9297 | 250.92 KB |
| MinimalJson    |  15.96 μs |   1.566 μs |  0.086 μs |  1.4648 | 0.0305 |  24.13 KB |
