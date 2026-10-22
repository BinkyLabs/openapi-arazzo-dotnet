namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoRuntimeExpressionTests
{
    [Theory]
    [InlineData("$url")]
    [InlineData("$method")]
    [InlineData("$statusCode")]
    [InlineData("$request.header.Accept")]
    [InlineData("$request.query.userId")]
    [InlineData("$request.path.petId")]
    [InlineData("$request.body#/user/uuid")]
    [InlineData("$response.headers.Location")]
    [InlineData("$response.body")]
    [InlineData("$response.body#/a~1b/m~0n")]
    [InlineData("$inputs.username")]
    [InlineData("$outputs.result")]
    [InlineData("$steps.lookup.outputs.id")]
    [InlineData("$workflows.lookup.outputs.id")]
    [InlineData("$workflows.lookup")]
    [InlineData("$sourceDescriptions.api.url")]
    [InlineData("$components.parameters.page")]
    [InlineData("$components.successActions.notify")]
    [InlineData("$message.payload.orderId")]
    [InlineData("$message.payload#/id")]
    [InlineData("$message.header.correlationId")]
    [InlineData("$request.payload#/orderId")]
    [InlineData("$response.payload.orderId")]
    [InlineData("$self")]
    public void Build_AcceptsExpressionsFromBothArazzoVersions(string expression)
    {
        var result = ArazzoRuntimeExpression.Build(expression);

        Assert.Equal(expression, result.Expression);
        Assert.Equal(expression, result.ToString());
    }

    [Theory]
    [InlineData("url")]
    [InlineData(" ")]
    [InlineData("$")]
    [InlineData("$URL")]
    [InlineData("$invalid")]
    [InlineData("$inputs")]
    [InlineData("$request.header.")]
    [InlineData("$response.statusCode")]
    [InlineData("$request.body#/bad~2pointer")]
    [InlineData("$response.body.id")]
    [InlineData("prefix {$inputs.id}")]
    [InlineData("{$request.query.id}")]
    public void Build_RejectsLiteralsInvalidExpressionsAndOpenApiCompositeExpressions(string expression)
    {
        var exception = Assert.Throws<ArazzoException>(() => ArazzoRuntimeExpression.Build(expression));

        Assert.Contains(expression, exception.Message, StringComparison.Ordinal);
        Assert.Contains("invalid format", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_RejectsNullAndEmptyExpressions()
    {
        var nullException = Assert.Throws<ArgumentNullException>(() => ArazzoRuntimeExpression.Build(null!));
        Assert.Equal("expression", nullException.ParamName);
        var emptyException = Assert.Throws<ArgumentException>(() => ArazzoRuntimeExpression.Build(""));
        Assert.Equal("expression", emptyException.ParamName);
    }

    [Fact]
    public void Equality_UsesExpressionValueAndSupportsHashCollections()
    {
        var expression = ArazzoRuntimeExpression.Build("$inputs.userId");
        var sameExpression = ArazzoRuntimeExpression.Build("$inputs.userId");
        var differentlyCasedExpression = ArazzoRuntimeExpression.Build("$inputs.UserId");

        Assert.NotSame(expression, sameExpression);
        Assert.True(expression.Equals(sameExpression));
        Assert.True(expression.Equals((object)sameExpression));
        Assert.True(sameExpression.Equals(expression));
        Assert.Equal(expression.GetHashCode(), sameExpression.GetHashCode());
        Assert.False(expression.Equals(differentlyCasedExpression));
        Assert.False(expression.Equals((object)differentlyCasedExpression));
        Assert.False(expression.Equals((ArazzoRuntimeExpression?)null));
        Assert.False(expression.Equals((object?)null));
        Assert.False(expression.Equals("$inputs.userId"));
        var expressions = new HashSet<ArazzoRuntimeExpression> { expression, sameExpression, differentlyCasedExpression };
        Assert.Equal(2, expressions.Count);
    }
}