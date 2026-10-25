using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_DataAccess.Common.Models;
using Clinic_Management_System.DTOs.Auth;
using Clinic_Management_System.Options;
using Clinic_Management_System.Services;
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
        private readonly ITokenService _tokenService;

        private static readonly string _absentUserHash = BCrypt.Net.BCrypt.HashPassword("absent user");

        public AuthController(
            IUserRepository userRepository,
            IAuditRepository auditRepository,
            IOptions<JwtOptions> jwtOptions, 
            ILogger<AuthController> logger,
            ITokenService tokenService
        )
        {
            _userRepository = userRepository;
            _jwt = jwtOptions.Value;
            _logger = logger;
            _auditRepository = auditRepository;
            _tokenService = tokenService;
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

            // Step 2: Verify the provided password against the stored hash
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? _absentUserHash);
            
            if (user == null)
            {
                _logger.LogWarning("Failed login for unknown email {Email}", request.Email);
                await _auditRepository.CreateAsync(null, AuditAction.LoginFailed, detail: request.Email);

                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid credentials");
            }

            if (!isValidPassword)
            {
                _logger.LogWarning("Failed login for {Email}", request.Email);
                await _auditRepository.CreateAsync(null, AuditAction.LoginFailed, "User", user.UserID, request.Email);

                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid credentials");
            }

            if (!user.Active)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Account inactive, please contact an admin");

            TokenPair tokens = _tokenService.IssueFor(user);

            if (await user.Update([new AuditRow(user.UserID, AuditAction.LoginSucceeded, null, null)]))
            {
                // Step 10: Return the serialized JWT token to the client.
                // The client will send this token with future requests.

                _logger.LogInformation("Login succeeded for user {UserId}", user.UserID);

                return Ok(new TokenResponse
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
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
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Account inactive, please contact an admin");

            // Rotation: the old refresh token is replaced, not reused.
            TokenPair tokens = _tokenService.IssueFor(user);

            if (await user.Update())
                return Ok(new TokenResponse
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
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

            int? creatorId = CurrentUserId();

            if (await user.Create(creatorId, AuditAction.UserCreated, request.Email))
            {
                _logger.LogInformation("User {UserId} created by admin {AdminId}", user.UserID, creatorId);

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

            int? actorId = CurrentUserId();
            string action = actorId == user.UserID ? AuditAction.PasswordChanged : AuditAction.PasswordReset;

            ActionResult result = await _SetPassword(user, request.NewPassword,
                new AuditRow(actorId, action, "User", user.UserID));

            if (result is not NoContentResult)
                return result;

            _logger.LogInformation("{Action} for user {UserId} by {ActorId}", action, user.UserID, actorId);

            return result;
        }

        int? CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;

        private async Task<ActionResult> _SetPassword(clsUser user, string newPassword, AuditRow auditRow)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // End Existing session
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            user.RefreshTokenRevokedAt = DateTime.UtcNow;

            if (await user.Update([auditRow]))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The password could not be saved.");
        }

    }
}
