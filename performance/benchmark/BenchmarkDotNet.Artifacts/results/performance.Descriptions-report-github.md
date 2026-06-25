```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean       | Error      | StdDev    | Gen0    | Gen1    | Allocated |
|--------------- |-----------:|-----------:|----------:|--------:|--------:|----------:|
| FormalBnplYaml | 254.236 μs | 108.672 μs | 5.9567 μs | 97.1680 |  1.9531 | 398.01 KB |
| FormalBnplJson | 107.880 μs |  19.413 μs | 1.0641 μs | 61.5234 |  1.4648 | 253.05 KB |
| FapiParYaml    | 214.135 μs | 149.913 μs | 8.2173 μs | 86.9141 | 15.6250 | 356.91 KB |
| FapiParJson    |  99.157 μs | 112.287 μs | 6.1548 μs | 60.0586 |  0.4883 | 246.51 KB |
| MinimalJson    |   9.376 μs |   2.744 μs | 0.1504 μs |  5.8594 |       - |  24.16 KB |
