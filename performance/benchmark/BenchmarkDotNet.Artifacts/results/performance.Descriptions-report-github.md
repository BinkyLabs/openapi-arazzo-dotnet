```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 8.0.28 (8.0.28, 8.0.2826.26413), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 8.0.28 (8.0.28, 8.0.2826.26413), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev   | Gen0    | Gen1    | Allocated |
|--------------- |----------:|-----------:|---------:|--------:|--------:|----------:|
| FormalBnplYaml | 331.65 μs |  14.443 μs | 0.792 μs | 92.2852 |       - | 378.23 KB |
| FormalBnplJson | 103.48 μs |  10.468 μs | 0.574 μs | 57.1289 | 11.7188 | 234.96 KB |
| FapiParYaml    | 250.53 μs |  47.477 μs | 2.602 μs | 85.9375 |       - | 351.38 KB |
| FapiParJson    | 103.99 μs | 151.132 μs | 8.284 μs | 59.0820 |  1.9531 | 242.17 KB |
| MinimalJson    |  10.51 μs |   0.905 μs | 0.050 μs |  5.2338 |       - |  21.38 KB |
