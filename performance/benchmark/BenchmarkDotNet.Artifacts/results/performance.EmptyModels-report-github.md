```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.11 (10.0.11, 10.0.1126.37416), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean       | Error     | StdDev    | Gen0   | Allocated |
|----------------------------- |-----------:|----------:|----------:|-------:|----------:|
| EmptyComponent               |   2.092 ns | 1.1690 ns | 0.0641 ns | 0.0134 |      56 B |
| EmptyCriterion               |   1.763 ns | 0.6441 ns | 0.0353 ns | 0.0115 |      48 B |
| EmptyCriterionExpressionType |   1.694 ns | 1.8029 ns | 0.0988 ns | 0.0096 |      40 B |
| EmptyDocument                | 173.863 ns | 5.8735 ns | 0.3219 ns | 0.2563 |    1072 B |
| EmptyFailureAction           |   2.978 ns | 2.2648 ns | 0.1241 ns | 0.0249 |     104 B |
| EmptyFailureActionReference  |  12.721 ns | 0.2955 ns | 0.0162 ns | 0.0325 |     136 B |
| EmptyInfo                    |   2.079 ns | 2.8910 ns | 0.1585 ns | 0.0134 |      56 B |
| EmptyInput                   |   9.634 ns | 3.2238 ns | 0.1767 ns | 0.1090 |     456 B |
| EmptyInputReference          |  13.561 ns | 1.4493 ns | 0.0794 ns | 0.0440 |     184 B |
| EmptyParameter               |   1.806 ns | 1.8777 ns | 0.1029 ns | 0.0115 |      48 B |
| EmptyParameterReference      |  12.674 ns | 3.4329 ns | 0.1882 ns | 0.0344 |     144 B |
| EmptyPayloadReplacement      |   1.586 ns | 0.1849 ns | 0.0101 ns | 0.0096 |      40 B |
| EmptyRequestBody             |   1.786 ns | 0.6092 ns | 0.0334 ns | 0.0115 |      48 B |
| EmptySourceDescription       |   1.862 ns | 1.8955 ns | 0.1039 ns | 0.0115 |      48 B |
| EmptyStep                    |   3.020 ns | 1.0314 ns | 0.0565 ns | 0.0268 |     112 B |
| EmptySuccessAction           |   2.065 ns | 0.1563 ns | 0.0086 ns | 0.0153 |      64 B |
| EmptySuccessActionReference  |  13.094 ns | 4.6286 ns | 0.2537 ns | 0.0325 |     136 B |
| EmptyWorkflow                |   2.836 ns | 0.8678 ns | 0.0476 ns | 0.0249 |     104 B |
