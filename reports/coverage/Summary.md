# Summary

|||
|:---|:---|
| Generated on: | 03/18/2026 - 13:41:05 |
| Coverage date: | 03/18/2026 - 13:32:33 - 03/18/2026 - 13:40:46 |
| Parser: | MultiReport (2x Cobertura) |
| Assemblies: | 1 |
| Classes: | 39 |
| Files: | 54 |
| **Line coverage:** | 60.3% (714 of 1184) |
| Covered lines: | 714 |
| Uncovered lines: | 470 |
| Coverable lines: | 1184 |
| Total lines: | 3514 |
| **Branch coverage:** | 35.7% (146 of 408) |
| Covered branches: | 146 |
| Total branches: | 408 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

# Risk Hotspots

| **Assembly** | **Class** | **Method** | **Crap Score** | **Cyclomatic complexity** |
|:---|:---|:---|---:|---:|
| BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.ArazzoModelFactory | RetrieveStreamAndFormatAsync() | 156 | 12 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.BaseArazzoReference | get_ReferenceV1() | 156 | 12 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.ArazzoModelFactory | LoadFromStreamAsync() | 110 | 10 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.BaseArazzoReference | SetJsonPointerPath(...) | 110 | 10 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.Reader.SourceGenerationContext | ExpandConverter(...) | 110 | 10 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.Reader.V1.ArazzoV1Deserializer | LoadSchema(...) | 110 | 10 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.ArazzoModelFactory | PrepareStreamForReadingAsync() | 72 | 8 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.BaseArazzoReference | ResolveRelativePointer(...) | 72 | 8 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.Reader.MapNode | CreateArrayMap(...) | 42 | 6 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.Reader.MapNode | GetScalarValue(...) | 42 | 6 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext | GetFromTempStorage(...) | 42 | 6 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext | SetTempStorage(...) | 42 | 6 || BinkyLabs.OpenApi.Arazzo | BinkyLabs.OpenApi.Arazzo.Reader.SourceGenerationContext | GetRuntimeConverterForType(...) | 42 | 6 |
# Coverage

| **Name** | **Covered** | **Uncovered** | **Coverable** | **Total** | **Line coverage** | **Covered** | **Total** | **Branch coverage** |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| **BinkyLabs.OpenApi.Arazzo** | **714** | **470** | **1184** | **3514** | **60.3%** | **146** | **408** | **35.7%** |
| BinkyLabs.OpenApi.Arazzo.ArazzoComponent | 9 | 5 | 14 | 66 | 64.2% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.ArazzoCriterion | 15 | 4 | 19 | 73 | 78.9% | 11 | 12 | 91.6% |
| BinkyLabs.OpenApi.Arazzo.ArazzoCriterionExpressionType | 13 | 3 | 16 | 55 | 81.2% | 8 | 8 | 100% |
| BinkyLabs.OpenApi.Arazzo.ArazzoDocument | 15 | 11 | 26 | 116 | 57.6% | 10 | 10 | 100% |
| BinkyLabs.OpenApi.Arazzo.ArazzoException | 4 | 3 | 7 | 46 | 57.1% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.ArazzoExtensibleExtensions | 7 | 1 | 8 | 35 | 87.5% | 4 | 6 | 66.6% |
| BinkyLabs.OpenApi.Arazzo.ArazzoFailureAction | 10 | 2 | 12 | 42 | 83.3% | 4 | 4 | 100% |
| BinkyLabs.OpenApi.Arazzo.ArazzoInfo | 6 | 3 | 9 | 36 | 66.6% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.ArazzoJsonReader | 22 | 22 | 44 | 118 | 50% | 10 | 14 | 71.4% |
| BinkyLabs.OpenApi.Arazzo.ArazzoModelFactory | 21 | 72 | 93 | 253 | 22.5% | 9 | 52 | 17.3% |
| BinkyLabs.OpenApi.Arazzo.ArazzoParameter | 11 | 5 | 16 | 48 | 68.7% | 1 | 2 | 50% |
| BinkyLabs.OpenApi.Arazzo.ArazzoPayloadReplacement | 9 | 3 | 12 | 43 | 75% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.ArazzoReaderException | 0 | 6 | 6 | 41 | 0% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.ArazzoReaderSettings | 9 | 19 | 28 | 102 | 32.1% | 1 | 8 | 12.5% |
| BinkyLabs.OpenApi.Arazzo.ArazzoRequestBody | 10 | 4 | 14 | 51 | 71.4% | 0 | 2 | 0% |
| BinkyLabs.OpenApi.Arazzo.ArazzoResultAction<T> | 12 | 6 | 18 | 64 | 66.6% | 8 | 8 | 100% |
| BinkyLabs.OpenApi.Arazzo.ArazzoSerializationException | 1 | 2 | 3 | 28 | 33.3% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.ArazzoSourceDescription | 11 | 4 | 15 | 49 | 73.3% | 3 | 4 | 75% |
| BinkyLabs.OpenApi.Arazzo.ArazzoStep | 26 | 13 | 39 | 126 | 66.6% | 7 | 16 | 43.7% |
| BinkyLabs.OpenApi.Arazzo.ArazzoSuccessAction | 6 | 0 | 6 | 27 | 100% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.ArazzoWorkflow | 15 | 10 | 25 | 102 | 60% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.ArazzoYamlReader | 13 | 21 | 34 | 103 | 38.2% | 3 | 8 | 37.5% |
| BinkyLabs.OpenApi.Arazzo.BaseArazzoReference | 0 | 44 | 44 | 150 | 0% | 0 | 34 | 0% |
| BinkyLabs.OpenApi.Arazzo.JsonNodeExtension | 5 | 1 | 6 | 36 | 83.3% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.Reader.ArazzoDiagnostic | 0 | 3 | 3 | 27 | 0% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.Reader.JsonNodeHelper | 0 | 2 | 2 | 21 | 0% | 0 | 4 | 0% |
| BinkyLabs.OpenApi.Arazzo.Reader.ListNode | 13 | 9 | 22 | 73 | 59% | 4 | 6 | 66.6% |
| BinkyLabs.OpenApi.Arazzo.Reader.MapNode | 51 | 49 | 100 | 209 | 51% | 11 | 54 | 20.3% |
| BinkyLabs.OpenApi.Arazzo.Reader.ParseNode | 8 | 15 | 23 | 90 | 34.7% | 5 | 6 | 83.3% |
| BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext | 26 | 44 | 70 | 253 | 37.1% | 8 | 32 | 25% |
| BinkyLabs.OpenApi.Arazzo.Reader.PropertyNode | 21 | 17 | 38 | 100 | 55.2% | 8 | 8 | 100% |
| BinkyLabs.OpenApi.Arazzo.Reader.RootNode | 6 | 1 | 7 | 47 | 85.7% | 1 | 2 | 50% |
| BinkyLabs.OpenApi.Arazzo.Reader.SourceGenerationContext | 0 | 35 | 35 | 139 | 0% | 0 | 24 | 0% |
| BinkyLabs.OpenApi.Arazzo.Reader.V1.ArazzoV1Deserializer | 290 | 12 | 302 | 508 | 96% | 12 | 54 | 22.2% |
| BinkyLabs.OpenApi.Arazzo.Reader.V1.ArazzoV1VersionService | 18 | 5 | 23 | 54 | 78.2% | 0 | 4 | 0% |
| BinkyLabs.OpenApi.Arazzo.Reader.ValueNode | 9 | 1 | 10 | 39 | 90% | 2 | 4 | 50% |
| BinkyLabs.OpenApi.Arazzo.ReadResult | 0 | 7 | 7 | 38 | 0% | 0 | 0 |  |
| BinkyLabs.OpenApi.Arazzo.StringExtensions | 15 | 5 | 20 | 66 | 75% | 11 | 16 | 68.7% |
| BinkyLabs.OpenApi.Arazzo.Writers.OpenApiWriterAnyExtensions | 7 | 1 | 8 | 40 | 87.5% | 5 | 6 | 83.3% |

