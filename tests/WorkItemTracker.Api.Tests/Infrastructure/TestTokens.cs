using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace WorkItemTracker.Api.Tests.Infrastructure;

public static class TestTokens
{
    public const string Issuer = "workitemtracker-tests";
    public const string Audience = "workitemtracker-api";

    // HS256 needs a key of at least 256 bits.
    public static readonly byte[] SigningKey =
        Encoding.UTF8.GetBytes("test-signing-key-for-integration-tests-only");

    public static string For(string userId) =>
        Create(new Claim(JwtRegisteredClaimNames.Sub, userId));

    // Correctly signed and otherwise valid, but carries no user id.
    public static string WithoutSubject() =>
        Create(new Claim(JwtRegisteredClaimNames.Name, "no-subject"));

    private static string Create(Claim claim)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity([claim]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(SigningKey), SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
