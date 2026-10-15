using Microsoft.AspNetCore.Http.HttpResults;

namespace Server.Endpoints;

/// <summary>Shared 404 shapes so every endpoint reports missing entities alike.</summary>
internal static class EndpointProblems
{
    public static ProblemHttpResult NotFound(string kind, string id)
        => TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            detail: $"{kind} '{id}' was not found.");
}
