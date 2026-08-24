```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat) (container)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean         | Error      | StdDev    | Gen0   | Allocated |
|----------------------------- |-------------:|-----------:|----------:|-------:|----------:|
| EmptyComponent               |     7.065 ns |  1.0742 ns | 0.0589 ns | 0.0033 |      56 B |
| EmptyCriterion               |     7.195 ns |  2.1019 ns | 0.1152 ns | 0.0029 |      48 B |
| EmptyCriterionExpressionType |     7.063 ns |  6.9140 ns | 0.3790 ns | 0.0024 |      40 B |
| EmptyDocument                | 1,238.359 ns | 60.8090 ns | 3.3331 ns | 0.0629 |    1072 B |
| EmptyFailureAction           |     8.122 ns |  2.4322 ns | 0.1333 ns | 0.0062 |     104 B |
| EmptyFailureActionReference  |    32.616 ns | 11.7514 ns | 0.6441 ns | 0.0081 |     136 B |
| EmptyInfo                    |     6.965 ns |  3.2295 ns | 0.1770 ns | 0.0033 |      56 B |
| EmptyInput                   |    17.784 ns |  1.5233 ns | 0.0835 ns | 0.0272 |     456 B |
| EmptyInputReference          |    32.169 ns |  9.1446 ns | 0.5012 ns | 0.0110 |     184 B |
| EmptyParameter               |     7.103 ns |  2.4092 ns | 0.1321 ns | 0.0029 |      48 B |
| EmptyParameterReference      |    29.813 ns | 10.0757 ns | 0.5523 ns | 0.0086 |     144 B |
| EmptyPayloadReplacement      |     6.569 ns |  1.1330 ns | 0.0621 ns | 0.0024 |      40 B |
| EmptyRequestBody             |     7.513 ns |  6.2333 ns | 0.3417 ns | 0.0029 |      48 B |
| EmptySourceDescription       |     8.159 ns |  6.8830 ns | 0.3773 ns | 0.0029 |      48 B |
| EmptyStep                    |     8.438 ns |  4.6398 ns | 0.2543 ns | 0.0067 |     112 B |
| EmptySuccessAction           |     7.536 ns |  4.7371 ns | 0.2597 ns | 0.0038 |      64 B |
| EmptySuccessActionReference  |    29.975 ns | 16.1984 ns | 0.8879 ns | 0.0081 |     136 B |
| EmptyWorkflow                |     8.892 ns |  0.2913 ns | 0.0160 ns | 0.0062 |     104 B |
