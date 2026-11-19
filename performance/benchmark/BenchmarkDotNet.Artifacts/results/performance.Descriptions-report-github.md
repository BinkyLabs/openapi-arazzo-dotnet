```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Gen0    | Gen1   | Allocated |
|--------------- |----------:|----------:|----------:|--------:|-------:|----------:|
| FormalBnplYaml | 768.11 μs | 863.66 μs | 47.340 μs | 31.2500 | 7.8125 |  524.7 KB |
| FormalBnplJson | 454.71 μs | 940.36 μs | 51.544 μs | 21.4844 | 5.8594 | 355.66 KB |
| FapiParYaml    | 497.10 μs | 566.34 μs | 31.043 μs | 27.3438 | 3.9063 | 466.46 KB |
| FapiParJson    | 351.58 μs | 544.45 μs | 29.843 μs | 19.5313 | 3.9063 | 346.26 KB |
| MinimalJson    |  38.87 μs |  94.14 μs |  5.160 μs |  1.9531 |      - |  32.35 KB |
