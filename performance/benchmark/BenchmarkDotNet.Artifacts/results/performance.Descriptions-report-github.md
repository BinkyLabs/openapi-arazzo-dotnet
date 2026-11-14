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
| FormalBnplYaml | 482.24 μs | 287.420 μs | 15.754 μs | 125.0000 | 27.3438 | 516.43 KB |
| FormalBnplJson | 190.25 μs | 101.451 μs |  5.561 μs |  83.9844 |  9.7656 | 344.96 KB |
| FapiParYaml    | 312.67 μs | 346.516 μs | 18.994 μs | 111.3281 | 21.4844 | 461.64 KB |
| FapiParJson    | 169.42 μs | 394.696 μs | 21.635 μs |  83.0078 |  0.9766 | 340.89 KB |
| MinimalJson    |  11.93 μs |   0.600 μs |  0.033 μs |   7.4463 |       - |  30.44 KB |
