```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.11 (10.0.11, 10.0.1126.37416), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean       | Error       | StdDev    | Gen0   | Allocated |
|----------------------------- |-----------:|------------:|----------:|-------:|----------:|
| EmptyComponent               |   2.028 ns |   0.5693 ns | 0.0312 ns | 0.0134 |      56 B |
| EmptyCriterion               |   1.970 ns |   3.7242 ns | 0.2041 ns | 0.0115 |      48 B |
| EmptyCriterionExpressionType |   1.600 ns |   1.6812 ns | 0.0922 ns | 0.0096 |      40 B |
| EmptyDocument                | 182.662 ns | 107.9960 ns | 5.9196 ns | 0.2563 |    1072 B |
| EmptyFailureAction           |   3.236 ns |   2.9785 ns | 0.1633 ns | 0.0249 |     104 B |
| EmptyFailureActionReference  |  13.118 ns |   3.5414 ns | 0.1941 ns | 0.0325 |     136 B |
| EmptyInfo                    |   1.957 ns |   1.1924 ns | 0.0654 ns | 0.0134 |      56 B |
| EmptyInput                   |  10.104 ns |  12.0979 ns | 0.6631 ns | 0.1090 |     456 B |
| EmptyInputReference          |  14.083 ns |  13.9007 ns | 0.7619 ns | 0.0440 |     184 B |
| EmptyParameter               |   1.790 ns |   0.2576 ns | 0.0141 ns | 0.0115 |      48 B |
| EmptyParameterReference      |  12.983 ns |   2.4555 ns | 0.1346 ns | 0.0344 |     144 B |
| EmptyPayloadReplacement      |   1.585 ns |   0.3521 ns | 0.0193 ns | 0.0096 |      40 B |
| EmptyRequestBody             |   1.796 ns |   0.0502 ns | 0.0028 ns | 0.0115 |      48 B |
| EmptySourceDescription       |   1.813 ns |   0.5952 ns | 0.0326 ns | 0.0115 |      48 B |
| EmptyStep                    |   3.117 ns |   0.5630 ns | 0.0309 ns | 0.0268 |     112 B |
| EmptySuccessAction           |   2.104 ns |   0.4752 ns | 0.0260 ns | 0.0153 |      64 B |
| EmptySuccessActionReference  |  12.601 ns |   1.7108 ns | 0.0938 ns | 0.0325 |     136 B |
| EmptyWorkflow                |   3.953 ns |   1.9834 ns | 0.1087 ns | 0.0249 |     104 B |
