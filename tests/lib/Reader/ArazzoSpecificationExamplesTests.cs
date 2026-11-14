// Licensed under the MIT license.

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoSpecificationExamplesTests
{
    private static readonly string SampleRootDirectory = Path.Combine(AppContext.BaseDirectory, "Samples");
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> KnownSampleErrors =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Join("DescribingApiWorkflowsWithArazzo", "specs", "error-bnpl.arazzo.yaml")] =
            [
                "#/workflows/steps/outputs/eligibilityCheckRequired: Output values must be valid runtime expressions or Selector Objects.",
                "#/workflows/steps/outputs/eligibleProducts: Output values must be valid runtime expressions or Selector Objects.",
                "#/workflows/steps/outputs/totalLoanAmount: Output values must be valid runtime expressions or Selector Objects.",
                "#/workflows/steps/outputs/customer: Output values must be valid runtime expressions or Selector Objects.",
                "#/workflows/steps/outputs/redirectAuthToken: Output values must be valid runtime expressions or Selector Objects.",
                "#/workflows/steps/outputs/loanTransactionId: Output values must be valid runtime expressions or Selector Objects.",
            ],
            [Path.Join("DescribingApiWorkflowsWithArazzo", "specs", "formal-bnpl.arazzo.yaml")] =
            [
                "#/workflows/steps/outputs/eligibilityCheckRequired: Output values must be valid runtime expressions or Selector Objects.",
                "#/workflows/steps/outputs/eligibleProducts: Output values must be valid runtime expressions or Selector Objects.",
                "#/workflows/steps/outputs/totalLoanAmount: Output values must be valid runtime expressions or Selector Objects.",
                "#/workflows/steps/outputs/customer: Output values must be valid runtime expressions or Selector Objects.",
                "#/workflows/steps/outputs/redirectAuthToken: Output values must be valid runtime expressions or Selector Objects.",
                "#/workflows/steps/outputs/loanTransactionId: Output values must be valid runtime expressions or Selector Objects.",
            ],
            [Path.Join("OperationResolution", "ambiguous.arazzo.yaml")] =
            [
                ": Workflow 'wf' step 'ambiguousOperationId' operationId 'listPets' is ambiguous because multiple non-arazzo sourceDescriptions are defined; use '$sourceDescriptions.<name>.listPets' syntax.",
            ],
        };

    [Fact]
    public async Task LoadFormUrlAsync_CopiedArazzoSampleDatasets_ParseWithOnlyKnownErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var settings = new ArazzoReaderSettings();
        settings.OpenApiSettings.RuleSet = ValidationRuleSet.GetEmptyRuleSet();

        Assert.True(Directory.Exists(SampleRootDirectory), $"Sample root directory was not found: {SampleRootDirectory}");

        var failures = new List<string>();

        foreach (var samplePath in Directory.EnumerateFiles(SampleRootDirectory, "*.arazzo.yaml", SearchOption.AllDirectories)
                     .Where(static path => new FileInfo(path).Length > 0)
                     .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var result = await ArazzoModelFactory.LoadFormUrlAsync(samplePath, settings, ct);
            var errors = result.Diagnostic?.Errors ?? [];
            var displayPath = Path.GetRelativePath(SampleRootDirectory, samplePath);
            var actualErrors = errors.Select(static error => $"{error.Pointer}: {error.Message}").ToArray();

            if (result.Document is null)
            {
                failures.Add(
                    $"""
                    {displayPath}
                    Document parsed: False
                    Errors:
                    {string.Join(Environment.NewLine, actualErrors)}
                    """);

                continue;
            }

            if (KnownSampleErrors.TryGetValue(displayPath, out var expectedErrors))
            {
                if (!actualErrors.SequenceEqual(expectedErrors))
                {
                    failures.Add(
                        $"""
                        {displayPath}
                        Document parsed: True
                        Expected errors:
                        {string.Join(Environment.NewLine, expectedErrors)}
                        Actual errors:
                        {string.Join(Environment.NewLine, actualErrors)}
                        """);
                }

                continue;
            }

            if (actualErrors.Length != 0)
            {
                failures.Add(
                    $"""
                    {displayPath}
                    Document parsed: True
                    Errors:
                    {string.Join(Environment.NewLine, actualErrors)}
                    """);
            }
        }

        Assert.True(failures.Count == 0, $"One or more copied Arazzo sample datasets produced unexpected parse results:{Environment.NewLine}{Environment.NewLine}{string.Join($"{Environment.NewLine}{Environment.NewLine}", failures)}");
    }
}