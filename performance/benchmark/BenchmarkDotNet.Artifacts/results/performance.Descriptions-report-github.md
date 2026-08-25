```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.11 (10.0.11, 10.0.1126.37416), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean       | Error      | StdDev    | Gen0     | Gen1   | Allocated |
|--------------- |-----------:|-----------:|----------:|---------:|-------:|----------:|
| FormalBnplYaml | 274.170 μs |  13.774 μs | 0.7550 μs | 104.4922 | 1.9531 | 427.99 KB |
| FormalBnplJson |  97.562 μs |  17.855 μs | 0.9787 μs |  62.5000 | 3.4180 | 255.53 KB |
| FapiParYaml    | 209.083 μs | 133.086 μs | 7.2949 μs |  91.0645 | 0.4883 | 372.76 KB |
| FapiParJson    |  95.893 μs | 103.609 μs | 5.6792 μs |  61.0352 | 0.4883 | 251.14 KB |
| MinimalJson    |   8.217 μs |   1.618 μs | 0.0887 μs |   5.9052 |      - |  24.16 KB |
