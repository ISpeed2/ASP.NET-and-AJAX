using Microsoft.AspNetCore.Authorization;

namespace SecureFilesMvc.Web.Security;

public sealed class FileAccessRequirement(string role) : IAuthorizationRequirement
{
    public string Role { get; } = role;
}

public sealed class FileAccessHandler : AuthorizationHandler<FileAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, FileAccessRequirement requirement)
    {
        if (context.User.IsInRole(requirement.Role))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
