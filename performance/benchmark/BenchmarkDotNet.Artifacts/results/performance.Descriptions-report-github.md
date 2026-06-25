```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.43GHz), 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error        | StdDev     | Gen0    | Gen1   | Allocated |
|--------------- |----------:|-------------:|-----------:|--------:|-------:|----------:|
| FormalBnplYaml | 566.03 μs |   330.994 μs |  18.143 μs | 15.6250 | 1.9531 | 398.79 KB |
| FormalBnplJson | 230.63 μs |   935.121 μs |  51.257 μs |  9.7656 | 2.9297 | 253.52 KB |
| FapiParYaml    | 507.23 μs | 1,872.865 μs | 102.658 μs | 13.6719 | 1.9531 | 357.22 KB |
| FapiParJson    | 218.82 μs |   716.983 μs |  39.300 μs |  9.7656 | 1.9531 | 246.59 KB |
| MinimalJson    |  19.15 μs |     5.533 μs |   0.303 μs |  0.9766 |      - |  24.21 KB |
