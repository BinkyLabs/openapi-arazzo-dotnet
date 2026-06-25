```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean       | Error      | StdDev    | Gen0   | Allocated |
|----------------------------- |-----------:|-----------:|----------:|-------:|----------:|
| EmptyComponent               |   1.983 ns |  1.6009 ns | 0.0878 ns | 0.0134 |      56 B |
| EmptyCriterion               |   1.832 ns |  1.1693 ns | 0.0641 ns | 0.0115 |      48 B |
| EmptyCriterionExpressionType |   1.636 ns |  0.9960 ns | 0.0546 ns | 0.0096 |      40 B |
| EmptyDocument                | 185.559 ns | 35.6887 ns | 1.9562 ns | 0.2563 |    1072 B |
| EmptyFailureAction           |   3.075 ns |  1.2562 ns | 0.0689 ns | 0.0249 |     104 B |
| EmptyFailureActionReference  |  13.135 ns |  3.9928 ns | 0.2189 ns | 0.0325 |     136 B |
| EmptyInfo                    |   1.906 ns |  0.5317 ns | 0.0291 ns | 0.0134 |      56 B |
| EmptyInput                   |   9.779 ns |  4.0609 ns | 0.2226 ns | 0.1090 |     456 B |
| EmptyInputReference          |  14.023 ns |  6.8609 ns | 0.3761 ns | 0.0440 |     184 B |
| EmptyParameter               |   1.821 ns |  1.7714 ns | 0.0971 ns | 0.0115 |      48 B |
| EmptyParameterReference      |  13.514 ns |  4.8590 ns | 0.2663 ns | 0.0344 |     144 B |
| EmptyPayloadReplacement      |   1.918 ns |  3.8872 ns | 0.2131 ns | 0.0096 |      40 B |
| EmptyRequestBody             |   1.894 ns |  1.4989 ns | 0.0822 ns | 0.0115 |      48 B |
| EmptySourceDescription       |   1.858 ns |  1.3284 ns | 0.0728 ns | 0.0115 |      48 B |
| EmptyStep                    |   3.309 ns |  3.1451 ns | 0.1724 ns | 0.0268 |     112 B |
| EmptySuccessAction           |   2.214 ns |  1.2693 ns | 0.0696 ns | 0.0153 |      64 B |
| EmptySuccessActionReference  |  13.810 ns | 12.5719 ns | 0.6891 ns | 0.0325 |     136 B |
| EmptyWorkflow                |   2.971 ns |  3.0842 ns | 0.1691 ns | 0.0249 |     104 B |
