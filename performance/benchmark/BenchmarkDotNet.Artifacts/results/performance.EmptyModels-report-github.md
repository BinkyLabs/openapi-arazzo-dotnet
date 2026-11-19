```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean         | Error      | StdDev    | Gen0   | Allocated |
|----------------------------- |-------------:|-----------:|----------:|-------:|----------:|
| EmptyComponent               |     7.599 ns |  2.7451 ns | 0.1505 ns | 0.0033 |      56 B |
| EmptyCriterion               |     7.022 ns |  2.7660 ns | 0.1516 ns | 0.0029 |      48 B |
| EmptyCriterionExpressionType |     6.630 ns |  1.5219 ns | 0.0834 ns | 0.0024 |      40 B |
| EmptyDocument                | 1,054.562 ns | 11.3572 ns | 0.6225 ns | 0.0687 |    1168 B |
| EmptyFailureAction           |     9.219 ns |  4.6332 ns | 0.2540 ns | 0.0067 |     112 B |
| EmptyFailureActionReference  |    28.078 ns |  1.2122 ns | 0.0664 ns | 0.0081 |     136 B |
| EmptyInfo                    |     7.018 ns |  0.5166 ns | 0.0283 ns | 0.0033 |      56 B |
| EmptyInput                   |    18.292 ns |  5.9231 ns | 0.3247 ns | 0.0272 |     456 B |
| EmptyInputReference          |    28.425 ns | 12.4273 ns | 0.6812 ns | 0.0110 |     184 B |
| EmptyParameter               |     6.880 ns |  0.7516 ns | 0.0412 ns | 0.0029 |      48 B |
| EmptyParameterReference      |    27.192 ns |  1.0714 ns | 0.0587 ns | 0.0086 |     144 B |
| EmptyPayloadReplacement      |     7.391 ns |  1.5609 ns | 0.0856 ns | 0.0029 |      48 B |
| EmptyRequestBody             |     6.861 ns |  3.2605 ns | 0.1787 ns | 0.0029 |      48 B |
| EmptySourceDescription       |     8.377 ns |  4.1486 ns | 0.2274 ns | 0.0029 |      48 B |
| EmptyStep                    |    10.905 ns |  2.3400 ns | 0.1283 ns | 0.0091 |     152 B |
| EmptySuccessAction           |     8.666 ns |  3.6640 ns | 0.2008 ns | 0.0043 |      72 B |
| EmptySuccessActionReference  |    30.641 ns |  6.0242 ns | 0.3302 ns | 0.0081 |     136 B |
| EmptyWorkflow                |     9.502 ns |  2.4207 ns | 0.1327 ns | 0.0062 |     104 B |
