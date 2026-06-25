using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

using SharpYaml;

namespace BinkyLabs.OpenApi.Arazzo.Cli;

/// <summary>
/// Command line application for OpenAPI Arazzo documents.
/// </summary>
public static class ArazzoCliApp
{
    /// <summary>
    /// Runs the command line application.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="cancellationToken">Propagates notifications that operations should be cancelled.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var rootCommand = new RootCommand("BinkyLabs OpenAPI Arazzo CLI - Validate Arazzo documents");
        var validateCommand = CreateValidateCommand();
        rootCommand.Add(validateCommand);

        return await rootCommand.Parse(args).InvokeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static Command CreateValidateCommand()
    {
        var validateCommand = new Command("validate", "Validate an Arazzo document and report errors and warnings");
        var inputArgument = new Argument<string>("input") { Description = "Path or URL to the Arazzo document" };
        var warningsAsErrorsOption = CreateWarningsAsErrorsOption();

        validateCommand.Add(inputArgument);
        validateCommand.Add(warningsAsErrorsOption);

        validateCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = parseResult.GetValue(inputArgument);
            var warningsAsErrors = parseResult.GetValue(warningsAsErrorsOption);

            if (string.IsNullOrEmpty(input))
            {
                await Console.Error.WriteLineAsync("Error: Input argument is required.").ConfigureAwait(false);
                return 1;
            }

            return await ValidateArazzoAsync(input, warningsAsErrors, cancellationToken).ConfigureAwait(false);
        });

        return validateCommand;
    }

    private static Option<bool> CreateWarningsAsErrorsOption()
    {
        var warningsAsErrorsOption = new Option<bool>("--warnings-as-errors") { Description = "Treat warnings as errors" };
        return warningsAsErrorsOption;
    }

    private static async Task<int> ValidateArazzoAsync(string input, bool warningsAsErrors, CancellationToken cancellationToken)
    {
        try
        {
            await Console.Out.WriteLineAsync($"Validating Arazzo document: {input}").ConfigureAwait(false);

            if (!IsHttpUrl(input) && !File.Exists(input))
            {
                await Console.Error.WriteLineAsync($"Error: Arazzo file '{input}' does not exist.").ConfigureAwait(false);
                return 1;
            }

            var (_, diagnostic) = await ArazzoDocument.LoadFromUrlAsync(input, token: cancellationToken).ConfigureAwait(false);
            diagnostic ??= new ArazzoDiagnostic();

            DisplayDiagnostics("Errors", diagnostic.Errors, Console.Error);
            DisplayDiagnostics("Warnings", diagnostic.Warnings, warningsAsErrors ? Console.Error : Console.Out);

            if (diagnostic.Errors.Count > 0)
            {
                return 1;
            }

            if (warningsAsErrors && diagnostic.Warnings.Count > 0)
            {
                await Console.Error.WriteLineAsync("Warnings were treated as errors.").ConfigureAwait(false);
                return 1;
            }

            await Console.Out.WriteLineAsync("Arazzo document is valid.").ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException)
        {
            await Console.Out.WriteLineAsync("Operation was cancelled.").ConfigureAwait(false);
            return 130;
        }
        catch (UnauthorizedAccessException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
        catch (IOException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
        catch (ArazzoReaderException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
        catch (JsonException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
        catch (YamlException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
    }

    private static bool IsHttpUrl(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static void DisplayDiagnostics(string title, IList<OpenApiError> diagnostics, TextWriter writer)
    {
        if (diagnostics.Count == 0)
        {
            return;
        }

        writer.WriteLine($"{title}:");
        foreach (var diagnostic in diagnostics)
        {
            writer.WriteLine($"  - {FormatDiagnostic(diagnostic)}");
        }
    }

    private static string FormatDiagnostic(OpenApiError diagnostic)
    {
        return string.IsNullOrEmpty(diagnostic.Pointer) ?
            diagnostic.Message :
            $"{diagnostic.Pointer}: {diagnostic.Message}";
    }
}