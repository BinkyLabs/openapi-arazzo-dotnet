```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Gen0     | Gen1    | Allocated |
|--------------- |----------:|-----------:|----------:|---------:|--------:|----------:|
| FormalBnplYaml | 411.55 μs | 334.874 μs | 18.356 μs | 126.9531 |  3.9063 | 524.81 KB |
| FormalBnplJson | 207.48 μs | 464.948 μs | 25.485 μs |  86.9141 |  0.9766 | 355.92 KB |
| FapiParYaml    | 339.55 μs | 110.410 μs |  6.052 μs | 113.2813 | 15.6250 | 467.77 KB |
| FapiParJson    | 175.63 μs |  69.839 μs |  3.828 μs |  83.9844 | 16.6016 | 346.41 KB |
| MinimalJson    |  12.61 μs |   0.083 μs |  0.005 μs |   7.9651 |       - |  32.57 KB |
