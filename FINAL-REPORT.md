# Arazzo Support for .NET - ILF Grant Final Report

## Brief Project Description

[Arazzo](https://spec.openapis.org/arazzo/v1.1.0.html) is an open standard for describing sequences of API calls, their dependencies, and how data flows between steps. It enables API providers to describe complete workflows across one or more APIs, rather than only individual operations.

To enable [Kiota](https://github.com/microsoft/kiota) to generate client SDKs that support both atomic operations and multistep workflows, we first need a .NET library that can read and work with Arazzo descriptions. This grant funded that foundational library; generating the workflow orchestration code in Kiota is the next step.

## Project Update

The project ran from June to December 2026. The original scope covered Arazzo 1.0, and the grant was extended to add support for Arazzo 1.1 after the new specification version came out during the grant.

The result is a new .NET library supporting both Arazzo 1.0 and 1.1, with an object model, JSON and YAML readers, writers, static validation, and reference resolution. A companion command-line tool lets developers validate workflow descriptions without writing application code.

Because this is a new project, the main reference for the work is the [BinkyLabs/openapi-arazzo-dotnet repository](https://github.com/BinkyLabs/openapi-arazzo-dotnet), which contains the implementation, documentation, tests, and performance benchmarks.

The NuGet package pages are:

- [BinkyLabs.OpenApi.Arazzo](https://www.nuget.org/packages/BinkyLabs.OpenApi.Arazzo): the .NET library.
- [BinkyLabs.OpenApi.Arazzo.Tool](https://www.nuget.org/packages/BinkyLabs.OpenApi.Arazzo.Tool): the companion validation CLI.

## Project Impact & Target Audiences

The direct beneficiaries are .NET developers building API tooling, API providers documenting workflows, and SDK maintainers who want to move beyond generating clients for isolated endpoints. The library provides a reusable foundation instead of requiring each tool to implement its own Arazzo parser and object model.

For the Interledger ecosystem, workflows are particularly relevant to integrations that require several coordinated API calls. Describing those flows in a standard format is a prerequisite for generating consistent workflow-level SDK capabilities across languages, reducing the amount of orchestration that application developers must eventually write by hand.

The wider OpenAPI tooling community also benefits from the specification fixes and shared parsing improvements produced during this work.

## Progress on Objectives, Key Activities

| Area | Delivered |
|---|---|
| Arazzo 1.0 support | A .NET object model and APIs for reading, building, serializing, and statically validating workflow descriptions. |
| Arazzo 1.1 extension | Support for the new document fields and models, including selectors, document identity, querystring parameters, step dependencies, and AsyncAPI-related metadata. |
| Reference resolution | Local and opt-in external reference handling, document identity reuse, JSON Schema anchors, and validation of loaded operation and workflow targets. |
| Developer tooling | A validation CLI, GitHub Action integration, usage examples, and documentation. |
| Quality and performance | Automated regression tests, compatibility checks, and repeatable parsing and object-allocation benchmarks. |

This is a parser and tooling library, not a workflow execution engine. Arazzo 1.1 AsyncAPI support covers the document model and parsing/serialization; loading AsyncAPI descriptions and channel-aware validation remain follow-up work. Runtime expression and selector evaluation are also outside the library's current execution scope.

## Co-benefits

### Improvements to the Arazzo Specification

Implementing the standard uncovered inconsistencies that I contributed fixes for upstream. Four specification pull requests created during the grant were merged:

- [OAI/Arazzo-Specification#616](https://github.com/OAI/Arazzo-Specification/pull/616): corrected reusable object values to allow arbitrary values rather than restricting their type.
- [OAI/Arazzo-Specification#615](https://github.com/OAI/Arazzo-Specification/pull/615): added the missing `channel` parameter location.
- [OAI/Arazzo-Specification#614](https://github.com/OAI/Arazzo-Specification/pull/614): allowed arbitrary JSON values for payload replacements.
- [OAI/Arazzo-Specification#613](https://github.com/OAI/Arazzo-Specification/pull/613): added the missing `xpath-31` option to the expression version description.

These changes benefit all Arazzo implementers, not just this .NET library.

### Shared Parsing Performance

The work also contributed to a major parsing performance improvement in the shared infrastructure used for OpenAPI, Arazzo, and Overlay documents. [microsoft/OpenAPI.NET#2852](https://github.com/microsoft/OpenAPI.NET/pull/2852) removed the intermediate `ParseNode` wrapper infrastructure in favor of direct `System.Text.Json` node APIs, reducing intermediate allocations and improving parsing efficiency.

The pull request includes benchmark results showing substantial time and memory improvements. Its before-and-after measurements used different hardware and runtime versions, so the reported percentages should not be treated as a controlled comparison or a universal speedup.

## Communications and Marketing

Development took place publicly on GitHub, with implementation work, issue discussions, and reviews available in the project repository. The README and package documentation provide entry points for developers who want to use the library or validation tool.

The work was also included in the Interledger Foundation's announcement, [SDK Grant Funds Security Features, OpenAPI Specification, and Arazzo Workflow Support in Kiota](https://interledger.org/news/sdk-grant-funds-security-features-openapi-specification-and-arazzo-workflow-support-kiota).

## What's Next?

The next major step is implementing Arazzo-based orchestration code generation in Kiota across its supported languages. The library supplies the description-processing foundation; Kiota still needs to translate those descriptions into generated workflow code that coordinates calls and passes results between steps.

That work will need to address runtime expression and selector evaluation, dependency-aware orchestration, and the appropriate runtime support in each target language. The completed library should not be mistaken for already-delivered multistep SDK generation.

## Community Support

Developers can try the library and CLI with their own workflow descriptions, report issues, and contribute examples or improvements through the project repository. Real-world workflow examples and feedback on the desired shape of generated orchestration APIs will be especially useful for the next phase of Kiota integration.

## Additional Comments

A big thank you to the Interledger Foundation for sponsoring this work and making it possible to build the foundation for workflow-aware client SDKs. Thank you also to the Arazzo contributors for putting the specification together in the first place and giving the community a shared standard to build on.
