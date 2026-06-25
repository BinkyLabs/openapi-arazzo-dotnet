```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.43GHz), 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean       | Error      | StdDev    | Gen0   | Allocated |
|----------------------------- |-----------:|-----------:|----------:|-------:|----------:|
| EmptyComponent               |   8.110 ns |   4.804 ns | 0.2633 ns | 0.0022 |      56 B |
| EmptyCriterion               |   7.347 ns |   3.891 ns | 0.2133 ns | 0.0019 |      48 B |
| EmptyCriterionExpressionType |   7.566 ns |   5.328 ns | 0.2921 ns | 0.0016 |      40 B |
| EmptyDocument                | 815.476 ns | 112.523 ns | 6.1678 ns | 0.0420 |    1072 B |
| EmptyFailureAction           |  12.131 ns |  12.419 ns | 0.6807 ns | 0.0041 |     104 B |
| EmptyFailureActionReference  |  31.246 ns |  28.850 ns | 1.5814 ns | 0.0054 |     136 B |
| EmptyInfo                    |   9.009 ns |   2.904 ns | 0.1592 ns | 0.0022 |      56 B |
| EmptyInput                   |  30.797 ns |  26.945 ns | 1.4769 ns | 0.0181 |     456 B |
| EmptyInputReference          |  33.270 ns |  30.436 ns | 1.6683 ns | 0.0073 |     184 B |
| EmptyParameter               |   8.197 ns |   3.207 ns | 0.1758 ns | 0.0019 |      48 B |
| EmptyParameterReference      |  33.609 ns |  13.981 ns | 0.7664 ns | 0.0057 |     144 B |
| EmptyPayloadReplacement      |   7.064 ns |   4.278 ns | 0.2345 ns | 0.0016 |      40 B |
| EmptyRequestBody             |   8.630 ns |   3.675 ns | 0.2014 ns | 0.0019 |      48 B |
| EmptySourceDescription       |   8.127 ns |   4.243 ns | 0.2326 ns | 0.0019 |      48 B |
| EmptyStep                    |  12.501 ns |  12.878 ns | 0.7059 ns | 0.0044 |     112 B |
| EmptySuccessAction           |   8.374 ns |   6.771 ns | 0.3711 ns | 0.0025 |      64 B |
| EmptySuccessActionReference  |  32.357 ns |  15.159 ns | 0.8309 ns | 0.0054 |     136 B |
| EmptyWorkflow                |  12.360 ns |   4.714 ns | 0.2584 ns | 0.0041 |     104 B |
