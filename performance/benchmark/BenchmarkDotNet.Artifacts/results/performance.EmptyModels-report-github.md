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
| EmptyComponent               |   2.384 ns |  0.5933 ns | 0.0325 ns | 0.0134 |      56 B |
| EmptyCriterion               |   2.212 ns |  0.7727 ns | 0.0424 ns | 0.0115 |      48 B |
| EmptyCriterionExpressionType |   2.010 ns |  0.6928 ns | 0.0380 ns | 0.0096 |      40 B |
| EmptyDocument                | 222.950 ns | 13.7344 ns | 0.7528 ns | 0.3059 |    1280 B |
| EmptyFailureAction           |   3.747 ns |  0.6507 ns | 0.0357 ns | 0.0268 |     112 B |
| EmptyFailureActionReference  |  15.006 ns |  2.6917 ns | 0.1475 ns | 0.0325 |     136 B |
| EmptyInfo                    |   2.260 ns |  1.0237 ns | 0.0561 ns | 0.0134 |      56 B |
| EmptyInput                   |  11.039 ns |  3.0097 ns | 0.1650 ns | 0.1090 |     456 B |
| EmptyInputReference          |  16.423 ns |  1.0073 ns | 0.0552 ns | 0.0440 |     184 B |
| EmptyParameter               |   2.221 ns |  0.8551 ns | 0.0469 ns | 0.0115 |      48 B |
| EmptyParameterReference      |  15.251 ns |  1.2792 ns | 0.0701 ns | 0.0344 |     144 B |
| EmptyPayloadReplacement      |   2.192 ns |  0.0921 ns | 0.0050 ns | 0.0115 |      48 B |
| EmptyRequestBody             |   2.190 ns |  0.7266 ns | 0.0398 ns | 0.0115 |      48 B |
| EmptySourceDescription       |   2.145 ns |  1.3323 ns | 0.0730 ns | 0.0115 |      48 B |
| EmptyStep                    |   4.262 ns |  1.4951 ns | 0.0820 ns | 0.0363 |     152 B |
| EmptySuccessAction           |   2.519 ns |  0.3913 ns | 0.0215 ns | 0.0172 |      72 B |
| EmptySuccessActionReference  |  14.283 ns |  2.8599 ns | 0.1568 ns | 0.0325 |     136 B |
| EmptyWorkflow                |   3.158 ns |  1.4575 ns | 0.0799 ns | 0.0249 |     104 B |
