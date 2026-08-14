```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.11 (10.0.11, 10.0.1126.37416), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean       | Error       | StdDev    | Gen0    | Gen1    | Allocated |
|--------------- |-----------:|------------:|----------:|--------:|--------:|----------:|
| FormalBnplYaml | 250.710 μs |  25.4614 μs | 1.3956 μs | 97.1680 |  1.9531 | 398.25 KB |
| FormalBnplJson | 101.504 μs |  56.8950 μs | 3.1186 μs | 62.5000 |  3.4180 | 255.53 KB |
| FapiParYaml    | 201.637 μs | 170.6118 μs | 9.3518 μs | 86.9141 | 15.6250 | 357.04 KB |
| FapiParJson    |  94.159 μs | 156.8022 μs | 8.5949 μs | 61.0352 |  0.4883 | 251.14 KB |
| MinimalJson    |   8.182 μs |   0.4147 μs | 0.0227 μs |  5.9052 |       - |  24.16 KB |
