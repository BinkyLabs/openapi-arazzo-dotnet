using BinkyLabs.OpenApi.Arazzo.Validation;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Validation;

public class ArazzoRuntimeExpressionValidatorTests
{
    [Theory]
    [InlineData("$url")]
    [InlineData("$method")]
    [InlineData("$statusCode")]
    [InlineData("$request.header.accept")]
    [InlineData("$request.query.userId")]
    [InlineData("$request.path.petId")]
    [InlineData("$request.body#/user/uuid")]
    [InlineData("$response.body#/status")]
    [InlineData("$response.body#/")]
    [InlineData("$response.body#/a~1b")]
    [InlineData("$response.body#/m~0n")]
    [InlineData("$response.body#/~0~1")]
    [InlineData("$response.body#/~01")]
    [InlineData("$response.body#/foo/0")]
    [InlineData("$response.body")]
    [InlineData("$response.headers.Location")]
    [InlineData("$inputs.username")]
    [InlineData("$outputs.bar")]
    [InlineData("$steps.someStepId.outputs.pets#/0/id")]
    [InlineData("$workflows.foo.outputs.mappedResponse#/name")]
    [InlineData("$sourceDescriptions.petstoreDescription.url")]
    [InlineData("$components.parameters.foo")]
    [InlineData("$components.successActions.notify")]
    public void IsRuntimeExpression_WithValidExpression_ReturnsTrue(string expression)
    {
        Assert.True(ArazzoRuntimeExpressionValidator.IsRuntimeExpression(expression));
    }

    [Theory]
    [InlineData("url")]
    [InlineData("$request.header.")]
    [InlineData("$request.body#/bad~2pointer")]
    [InlineData("$request.body#/bad~pointer")]
    [InlineData("$request.body#/bad~")]
    [InlineData("$request.body#/m~n")]
    [InlineData("$response.body.eligibilityCheckRequired")]
    [InlineData("$inputs")]
    [InlineData("$")]
    [InlineData("")]
    public void IsRuntimeExpression_WithInvalidExpression_ReturnsFalse(string expression)
    {
        Assert.False(ArazzoRuntimeExpressionValidator.IsRuntimeExpression(expression));
    }

    [Theory]
    [InlineData("$components.parameters.page", ReferenceType.Parameter)]
    [InlineData("$components.successActions.notify", ReferenceType.SuccessAction)]
    [InlineData("$components.failureActions.retry", ReferenceType.FailureAction)]
    public void IsReusableObjectReference_WithMatchingComponentType_ReturnsTrue(string reference, ReferenceType referenceType)
    {
        Assert.True(ArazzoReusableObjectReferenceValidator.IsReusableObjectReference(reference, referenceType));
    }

    [Theory]
    [InlineData("$components.parameters.page")]
    [InlineData("$components.successActions.notify")]
    [InlineData("$components.failureActions.retry")]
    public void IsReusableObjectReference_WithReusableComponentReference_ReturnsTrue(string reference)
    {
        Assert.True(ArazzoReusableObjectReferenceValidator.IsReusableObjectReference(reference));
    }

    [Theory]
    [InlineData("$steps.getUser.outputs.userId", null)]
    [InlineData("$components.inputs.user", null)]
    [InlineData("$components.parameters.page", ReferenceType.SuccessAction)]
    [InlineData("$components.parameters", ReferenceType.Parameter)]
    [InlineData("external.json#$components.parameters.page", ReferenceType.Parameter)]
    public void IsReusableObjectReference_WithInvalidReference_ReturnsFalse(string reference, ReferenceType? referenceType)
    {
        Assert.False(ArazzoReusableObjectReferenceValidator.IsReusableObjectReference(reference, referenceType));
    }
}