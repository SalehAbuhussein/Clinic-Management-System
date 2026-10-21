using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Auth;
using Clinic_Management_System.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtOptions _jwt;
        private readonly IUserRepository _userRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserRepository userRepository,
            IAuditRepository auditRepository,
            IOptions<JwtOptions> jwtOptions, 
            ILogger<AuthController> logger
        )
        {
            _userRepository = userRepository;
            _jwt = jwtOptions.Value;
            _logger = logger;
            _auditRepository = auditRepository;
        }

        [HttpPost("login")]
        [EnableRateLimiting("AuthLimiter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Step 1: Find User by Email
            var user = await _userRepository.FindByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogWarning("Failed login for unknown email {Email}", request.Email);
                await _auditRepository.CreateAsync(null, AuditAction.LoginFailed, detail: request.Email);

                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid credentials");
            }

            // Step 2: Verify the provided password against the stored hash
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isValidPassword)
            {
                _logger.LogWarning("Failed login for {Email}", request.Email);
                await _auditRepository.CreateAsync(null, AuditAction.LoginFailed, "User", user.UserID, request.Email);

                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid credentials");
            }

            if (!user.Active)
                return Problem(statusCode: 403, detail: "Account inactive, please contact an admin");

            // Step 3: Create claims that represent the authenticated user's identity
            // These claims will be embedded inside the JWT.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString() ?? ""),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // Step 4: Create the symmetric security key used to sign the JWT.
            // This key must match the key used in JWT validation middleware.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

            // Step 5: Define the signing credentials.
            // This specifies the algorithm used to sign the token.
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Step 6: Create the JWT token.
            // The token includes issuer, audience, claims, expiration, and signature.
            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
                signingCredentials: creds
            );

            // Step 7: Serialize the JWT into a string
            // This is what the client will send in the Authorization header
            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            // Step 8: Create Refresh token (secure random)
            // Refresh token is used to request a new access token later
            var refreshToken = GenerateRefreshToken();

            // Step 9: Store refresh token securely (hash + expiry + not revoked).
            // We store the HASH only (never store the raw refresh token)
            user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
            user.RefreshTokenRevokedAt = null;

            if (await user.Update())
            {
                // Step 10: Return the serialized JWT token to the client.
                // The client will send this token with future requests.

                _logger.LogInformation("Login succeeded for user {UserId}", user.UserID);
                await _auditRepository.CreateAsync(user.UserID, AuditAction.LoginSucceeded);

                return Ok(new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                });
            }

            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "The session could not be created. Please try again.");
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("AuthLimiter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            clsUser? user = await _userRepository.FindByEmailAsync(request.Email);

            if (user == null)
                return Problem(statusCode: StatusCodes.Status401Unauthorized,
                    detail: "Invalid refresh request.");

            if (user.RefreshTokenRevokedAt != null)
            {
                _logger.LogWarning("Refresh attempted with a revoked token for user {UserId}, revoked at {RevokedAt}", user.UserID, user.RefreshTokenRevokedAt);
                await _auditRepository.CreateAsync(null, AuditAction.RefreshRejected, "User", user.UserID, $"revoked at {user.RefreshTokenRevokedAt:u}");

                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Refresh token is revoked.");
            }

            if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                await _auditRepository.CreateAsync(null, AuditAction.RefreshRejected, "User", user.UserID, $"expired at {user.RefreshTokenExpiresAt:u}");
                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Refresh token expired.");
            }

            if (string.IsNullOrEmpty(user.RefreshTokenHash))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid refresh request.");

            bool refreshValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash);
            if (!refreshValid)
            {
                _logger.LogWarning("Refresh token did not match the stored hash for user {UserId}", user.UserID);
                await _auditRepository.CreateAsync(null, AuditAction.RefreshRejected, "User", user.UserID, "refresh token did not match the stored hash");

                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid refresh token");
            }

            if (!user.Active)
                return Problem(statusCode: 403, detail: "Account inactive, please contact an admin");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString() ?? ""),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
                signingCredentials: creds
            );

            var newAccessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            // Rotation: replace refresh token
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
            user.RefreshTokenRevokedAt = null;

            if (await user.Update())
                return Ok(new TokenResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken
                });

            return Problem(statusCode: StatusCodes.Status500InternalServerError,
                detail: "The session could not be refreshed. Please try again.");
        }

        [HttpPost("logout")]
        [EnableRateLimiting("AuthLimiter")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            clsUser? user = await _userRepository.FindByEmailAsync(request.Email);
            if (user == null)
                return NoContent();

            if (string.IsNullOrEmpty(user.RefreshTokenHash))
                return NoContent();

            bool refreshValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash);
            if (!refreshValid)
                return NoContent();

            user.RefreshTokenRevokedAt = DateTime.UtcNow;

            if (!await user.Update())
                return NoContent();

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        [EnableRateLimiting("AuthLimiter")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _userRepository.FindByEmailAsync(request.Email) != null)
                return Problem(statusCode: StatusCodes.Status409Conflict, detail: "A user with this email already exists.");

            clsUser user = new clsUser();
            clsPerson person = new clsPerson();

            person.Firstname = request.Firstname;
            person.Midname = request.Midname;
            person.Thirdname = request.Thirdname;
            person.Lastname = request.Lastname;
            person.Phone = request.Phone;
            person.Gender = request.Gender!.Value;
            person.DateOfBirth = request.DateOfBirth!.Value;
            person.NationalNo = request.NationalNo;

            user.Email = request.Email;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.PersonInfo = person;

            //user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            //user.RefreshTokenRevokedAt = null;
            //user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(GenerateRefreshToken());

            if (await user.Create())
            {
                int? adminId = CurrentUserId();

                _logger.LogInformation("User {UserId} created by admin {AdminId}", user.UserID, adminId);
                await _auditRepository.CreateAsync(adminId, AuditAction.UserCreated, "User", user.UserID, request.Email);

                return CreatedAtRoute("GetUser", new { id = user.UserID }, null);
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The user could not be created. Check the name, phone, national number and date of birth.");
        }

        [Authorize]
        [HttpPatch("{id}/change-password", Name = "ChangePassword")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> ChangePassword(
            [Range(1, int.MaxValue)] int id, 
            UpdatePasswordRequest request,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsUser? user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found!");

            var authResult = await authorizationService.AuthorizeAsync(
                User,
                user,
                "ManageAccount"
            );

            if (!authResult.Succeeded)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found!");

            // An admin resets an account whose password nobody knows, so there is
            // none to ask for. Everyone else is changing their own and proves it.
            // A missing one simply fails: BCrypt throws on null rather than
            // returning false.
            if (!User.IsInRole("Admin") && !BCrypt.Net.BCrypt.Verify(request.CurrentPassword ?? "", user.PasswordHash))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The current password is wrong.");

            ActionResult result = await SetPassword(user, request.NewPassword);

            if (result is not NoContentResult)
                return result;

            int? actorId = CurrentUserId();
            string action = actorId == user.UserID ? AuditAction.PasswordChanged : AuditAction.PasswordReset;

            _logger.LogInformation("{Action} for user {UserId} by {ActorId}", action, user.UserID, actorId);
            await _auditRepository.CreateAsync(actorId, action, "User", user.UserID);

            return result;
        }

        int? CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;

        private async Task<ActionResult> SetPassword(clsUser user, string newPassword)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // End Existing session
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            user.RefreshTokenRevokedAt = DateTime.UtcNow;

            if (await user.Update())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The password could not be saved.");
        }

        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
