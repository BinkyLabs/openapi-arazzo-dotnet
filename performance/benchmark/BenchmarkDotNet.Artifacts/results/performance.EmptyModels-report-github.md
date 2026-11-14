```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
Snapdragon X 12-core X1E80100 3.40 GHz (Max: 3.42GHz), 1 CPU, 12 logical and 12 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean       | Error      | StdDev    | Gen0   | Allocated |
|----------------------------- |-----------:|-----------:|----------:|-------:|----------:|
| EmptyComponent               |   2.226 ns |  0.1962 ns | 0.0108 ns | 0.0134 |      56 B |
| EmptyCriterion               |   2.047 ns |  0.3040 ns | 0.0167 ns | 0.0115 |      48 B |
| EmptyCriterionExpressionType |   1.909 ns |  0.3863 ns | 0.0212 ns | 0.0096 |      40 B |
| EmptyDocument                | 228.241 ns | 25.4138 ns | 1.3930 ns | 0.3328 |    1392 B |
| EmptyFailureAction           |   3.483 ns |  0.3358 ns | 0.0184 ns | 0.0268 |     112 B |
| EmptyFailureActionReference  |  14.512 ns |  1.8705 ns | 0.1025 ns | 0.0325 |     136 B |
| EmptyInfo                    |   2.223 ns |  0.6720 ns | 0.0368 ns | 0.0134 |      56 B |
| EmptyInput                   |  10.907 ns |  2.6934 ns | 0.1476 ns | 0.1090 |     456 B |
| EmptyInputReference          |  15.732 ns |  0.7248 ns | 0.0397 ns | 0.0440 |     184 B |
| EmptyParameter               |   2.041 ns |  0.5608 ns | 0.0307 ns | 0.0115 |      48 B |
| EmptyParameterReference      |  14.644 ns |  2.7363 ns | 0.1500 ns | 0.0344 |     144 B |
| EmptyPayloadReplacement      |   2.069 ns |  1.2000 ns | 0.0658 ns | 0.0115 |      48 B |
| EmptyRequestBody             |   2.057 ns |  0.6098 ns | 0.0334 ns | 0.0115 |      48 B |
| EmptySourceDescription       |   2.079 ns |  0.4744 ns | 0.0260 ns | 0.0115 |      48 B |
| EmptyStep                    |   4.332 ns |  0.1735 ns | 0.0095 ns | 0.0363 |     152 B |
| EmptySuccessAction           |   2.632 ns |  0.9814 ns | 0.0538 ns | 0.0172 |      72 B |
| EmptySuccessActionReference  |  14.514 ns |  2.1367 ns | 0.1171 ns | 0.0325 |     136 B |
| EmptyWorkflow                |   2.977 ns |  1.8428 ns | 0.1010 ns | 0.0249 |     104 B |
