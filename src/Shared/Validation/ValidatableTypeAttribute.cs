// Workaround for .NET 10: ValidatableTypeAttribute from the
// Microsoft.Extensions.Validation package is experimental (ASP0029) in plain
// class libraries. The Web and Razor SDKs generate this embedded attribute
// automatically; Shared declares it manually with the exact namespace and
// name the validation source generator looks for. Revisit on .NET 11+,
// where the attribute is no longer experimental.
namespace Microsoft.Extensions.Validation.Embedded
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ValidatableTypeAttribute : Attribute
    {
    }
}
