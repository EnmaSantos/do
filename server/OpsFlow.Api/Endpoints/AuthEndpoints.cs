using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OpsFlow.Api.Security;
using OpsFlow.Application.Models;
using OpsFlow.Application.Security;
using OpsFlow.Infrastructure.Identity;

namespace OpsFlow.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/login", async (LoginRequest request, UserManager<OpsUser> users, TokenService tokens) =>
        {
            var user = await users.FindByEmailAsync(request.Email);
            if (user is null || !user.IsActive || !await users.CheckPasswordAsync(user, request.Password))
                return Results.Unauthorized();
            var roles = await users.GetRolesAsync(user);
            return Results.Ok(tokens.Create(user, roles));
        }).AllowAnonymous();

        group.MapPost("/register", async (RegisterRequest request, UserManager<OpsUser> users, TokenService tokens) =>
        {
            var user = new OpsUser
            {
                Id = Guid.NewGuid(), UserName = request.Email, Email = request.Email,
                DisplayName = request.DisplayName, EmailConfirmed = true
            };
            var result = await users.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return Results.ValidationProblem(result.Errors.GroupBy(x => x.Code)
                    .ToDictionary(x => x.Key, x => x.Select(error => error.Description).ToArray()));
            await users.AddToRoleAsync(user, Roles.Viewer);
            return Results.Created("/api/users/me", tokens.Create(user, [Roles.Viewer]));
        }).AllowAnonymous();

        app.MapGet("/api/users/me", async (ClaimsPrincipal principal, UserManager<OpsUser> users) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            var roles = await users.GetRolesAsync(user);
            return Results.Ok(new UserProfile(user.Id, user.Email!, user.DisplayName, roles.ToArray()));
        }).WithTags("Users").RequireAuthorization();

        return app;
    }
}
