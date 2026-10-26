using Clinic_Business.Entities;
using Clinic_Management_System.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Clinic_Management_System.Services
{
    /// <summary>
    /// An access token and the raw refresh token that goes with it. Only the
    /// hash of the refresh token is ever stored, so this is the one moment the
    /// raw value exists.
    /// </summary>
    public record TokenPair(string AccessToken, string RefreshToken);

    /// <summary>
    /// Mints the pair a signed-in user gets, and stamps the refresh fields on
    /// the account.
    ///
    /// Login and refresh both end here, so the claims, the lifetimes and the
    /// signing are written once. Neither caller saves the user: they decide
    /// what the write should record and call Update themselves.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _jwt;

        public TokenService(IOptions<JwtOptions> jwtOptions)
        {
            _jwt = jwtOptions.Value;
        }

        /// <summary>
        /// Builds a token pair for an account and sets its refresh fields,
        /// leaving the caller to persist them.
        /// </summary>
        /// <param name="user">The account signing in or refreshing.</param>
        /// <returns>The access token and the raw refresh token.</returns>
        public TokenPair IssueFor(clsUser user)
        {
            Claim[] claims =
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString() ?? ""),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            ];

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
                signingCredentials: credentials
            );

            string refreshToken = _GenerateRefreshToken();

            user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
            user.RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays);
            user.RefreshTokenRevokedAt = null;

            return new TokenPair(new JwtSecurityTokenHandler().WriteToken(token), refreshToken);
        }

        private static string _GenerateRefreshToken()
        {
            byte[] bytes = new byte[64];

            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            return Convert.ToBase64String(bytes);
        }
    }
}
