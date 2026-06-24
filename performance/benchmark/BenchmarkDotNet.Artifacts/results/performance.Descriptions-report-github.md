```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 8.0.28 (8.0.28, 8.0.2826.26413), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 8.0.28 (8.0.28, 8.0.2826.26413), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev   | Gen0    | Gen1    | Allocated |
|--------------- |----------:|----------:|---------:|--------:|--------:|----------:|
| BnplYaml       |        NA |        NA |       NA |      NA |      NA |        NA |
| BnplJson       |        NA |        NA |       NA |      NA |      NA |        NA |
| FormalBnplYaml | 331.32 μs | 41.120 μs | 2.254 μs | 92.2852 |       - | 378.23 KB |
| FormalBnplJson | 123.15 μs | 33.005 μs | 1.809 μs | 57.4951 | 11.7188 | 234.96 KB |
| FapiParYaml    | 250.01 μs | 14.075 μs | 0.772 μs | 85.9375 |       - | 351.38 KB |
| FapiParJson    |  92.89 μs | 11.905 μs | 0.653 μs | 59.0820 |  1.9531 | 242.17 KB |
| MinimalJson    |  10.52 μs |  0.779 μs | 0.043 μs |  5.2338 |       - |  21.38 KB |

Benchmarks with issues:
  Descriptions.BnplYaml: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  Descriptions.BnplJson: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
