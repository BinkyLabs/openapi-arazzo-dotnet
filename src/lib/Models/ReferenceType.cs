using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

public enum ReferenceType
{
    [Display("successActions")]
    SuccessAction,
    [Display("failureActions")]
    FailureAction,
    [Display("parameters")]
    Parameter,
    [Display("inputs")]
    Input,

}