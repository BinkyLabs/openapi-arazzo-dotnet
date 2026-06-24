using BenchmarkDotNet.Attributes;

using BinkyLabs.OpenApi.Arazzo;

namespace performance;

[MemoryDiagnoser]
[JsonExporter]
[ShortRunJob]
public class EmptyModels
{
    [Benchmark]
    public ArazzoComponent EmptyComponent()
    {
        return new ArazzoComponent();
    }

    [Benchmark]
    public ArazzoCriterion EmptyCriterion()
    {
        return new ArazzoCriterion();
    }

    [Benchmark]
    public ArazzoCriterionExpressionType EmptyCriterionExpressionType()
    {
        return new ArazzoCriterionExpressionType();
    }

    [Benchmark]
    public ArazzoDocument EmptyDocument()
    {
        return new ArazzoDocument();
    }

    [Benchmark]
    public ArazzoFailureAction EmptyFailureAction()
    {
        return new ArazzoFailureAction();
    }

    [Benchmark]
    public ArazzoFailureActionReference EmptyFailureActionReference()
    {
        return new ArazzoFailureActionReference("failureAction");
    }

    [Benchmark]
    public ArazzoInfo EmptyInfo()
    {
        return new ArazzoInfo();
    }

    [Benchmark]
    public ArazzoInput EmptyInput()
    {
        return new ArazzoInput();
    }

    [Benchmark]
    public ArazzoInputReference EmptyInputReference()
    {
        return new ArazzoInputReference("input");
    }

    [Benchmark]
    public ArazzoParameter EmptyParameter()
    {
        return new ArazzoParameter();
    }

    [Benchmark]
    public ArazzoParameterReference EmptyParameterReference()
    {
        return new ArazzoParameterReference("parameter");
    }

    [Benchmark]
    public ArazzoPayloadReplacement EmptyPayloadReplacement()
    {
        return new ArazzoPayloadReplacement();
    }

    [Benchmark]
    public ArazzoRequestBody EmptyRequestBody()
    {
        return new ArazzoRequestBody();
    }

    [Benchmark]
    public ArazzoSourceDescription EmptySourceDescription()
    {
        return new ArazzoSourceDescription();
    }

    [Benchmark]
    public ArazzoStep EmptyStep()
    {
        return new ArazzoStep();
    }

    [Benchmark]
    public ArazzoSuccessAction EmptySuccessAction()
    {
        return new ArazzoSuccessAction();
    }

    [Benchmark]
    public ArazzoSuccessActionReference EmptySuccessActionReference()
    {
        return new ArazzoSuccessActionReference("successAction");
    }

    [Benchmark]
    public ArazzoWorkflow EmptyWorkflow()
    {
        return new ArazzoWorkflow();
    }
}