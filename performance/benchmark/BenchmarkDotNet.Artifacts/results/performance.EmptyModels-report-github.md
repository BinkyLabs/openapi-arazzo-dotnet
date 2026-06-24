```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 8.0.28 (8.0.28, 8.0.2826.26413), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 8.0.28 (8.0.28, 8.0.2826.26413), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean       | Error      | StdDev    | Gen0   | Allocated |
|----------------------------- |-----------:|-----------:|----------:|-------:|----------:|
| EmptyComponent               |   2.205 ns |  1.5938 ns | 0.0874 ns | 0.0134 |      56 B |
| EmptyCriterion               |   2.037 ns |  0.5599 ns | 0.0307 ns | 0.0115 |      48 B |
| EmptyCriterionExpressionType |   1.792 ns |  0.0841 ns | 0.0046 ns | 0.0096 |      40 B |
| EmptyDocument                | 246.087 ns | 15.7372 ns | 0.8626 ns | 0.1817 |     760 B |
| EmptyFailureAction           |   3.171 ns |  0.1501 ns | 0.0082 ns | 0.0249 |     104 B |
| EmptyFailureActionReference  |  28.892 ns |  6.6464 ns | 0.3643 ns | 0.0325 |     136 B |
| EmptyInfo                    |   2.249 ns |  0.5538 ns | 0.0304 ns | 0.0134 |      56 B |
| EmptyInput                   |  10.300 ns |  1.8936 ns | 0.1038 ns | 0.1090 |     456 B |
| EmptyInputReference          |  28.389 ns |  1.8389 ns | 0.1008 ns | 0.0440 |     184 B |
| EmptyParameter               |   2.003 ns |  0.4904 ns | 0.0269 ns | 0.0115 |      48 B |
| EmptyParameterReference      |  28.272 ns |  2.1701 ns | 0.1189 ns | 0.0344 |     144 B |
| EmptyPayloadReplacement      |   1.969 ns |  1.0541 ns | 0.0578 ns | 0.0096 |      40 B |
| EmptyRequestBody             |   2.033 ns |  1.9218 ns | 0.1053 ns | 0.0115 |      48 B |
| EmptySourceDescription       |   2.026 ns |  0.1368 ns | 0.0075 ns | 0.0115 |      48 B |
| EmptyStep                    |   3.189 ns |  2.2342 ns | 0.1225 ns | 0.0268 |     112 B |
| EmptySuccessAction           |   2.583 ns |  1.4973 ns | 0.0821 ns | 0.0153 |      64 B |
| EmptySuccessActionReference  |  29.734 ns |  7.2575 ns | 0.3978 ns | 0.0325 |     136 B |
| EmptyWorkflow                |   3.426 ns |  0.2352 ns | 0.0129 ns | 0.0249 |     104 B |
