using Mootable.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mootable.Application.Interfaces;
using System.Security.Cryptography;

namespace Mootable.Application.Features.Auth.Commands.RequestPasswordReset;

/// <summary>
/// Handler for password reset request
/// </summary>
public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, RequestPasswordResetResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<RequestPasswordResetCommandHandler> _logger;
    private readonly string _frontendUrl = "http://localhost:3000"; // TODO: Move to configuration

    public RequestPasswordResetCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<RequestPasswordResetCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<RequestPasswordResetResponse> Handle(
        RequestPasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find user by email
            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

            if (user == null)
            {
                // Don't reveal whether the email exists
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                return new RequestPasswordResetResponse
                {
                    Success = true,
                    Message = "If an account with that email exists, a password reset link has been sent."
                };
            }

            // Check if there's a recent unexpired token
            var existingToken = await _unitOfWork.Repository<PasswordResetToken>()
                .GetQueryable()
                .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingToken != null && existingToken.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
            {
                // Rate limiting: Don't allow new token within 5 minutes
                return new RequestPasswordResetResponse
                {
                    Success = true,
                    Message = "A password reset link was recently sent. Please check your email."
                };
            }

            // Generate new token
            var token = GenerateSecureToken();
            var resetToken = new PasswordResetToken
            {
                Token = token,
                Email = user.Email,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1), // Token expires in 1 hour
                RequestedFromIP = request.ClientIP,
                RequestedUserAgent = request.UserAgent,
                IsUsed = false
            };

            await _unitOfWork.Repository<PasswordResetToken>().AddAsync(resetToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send email
            var resetUrl = $"{_frontendUrl}/reset-password?token={token}";
            await SendPasswordResetEmail(user.Email, user.DisplayName ?? user.Username, resetUrl);

            _logger.LogInformation("Password reset token created for user {UserId}", user.Id);

            var response = new RequestPasswordResetResponse
            {
                Success = true,
                Message = "If an account with that email exists, a password reset link has been sent."
            };

            // In development, include the token in response (for testing)
            #if DEBUG
                response.ResetToken = token;
            #endif

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset request for {Email}", request.Email);
            return new RequestPasswordResetResponse
            {
                Success = false,
                Message = "An error occurred processing your request. Please try again later."
            };
        }
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private async Task SendPasswordResetEmail(string email, string name, string resetUrl)
    {
        var subject = "Reset Your Mootable Password";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; background: #0a0a0a; color: #e0e0e0; padding: 20px;'>
                <div style='max-width: 600px; margin: 0 auto; background: #1a1a1a; border-radius: 10px; padding: 30px; border: 1px solid #ffd700;'>
                    <h1 style='color: #ffd700; text-align: center; font-size: 28px; margin-bottom: 20px;'>
                        Password Reset Request
                    </h1>

                    <p style='font-size: 16px; line-height: 1.6;'>Hello {name},</p>

                    <p style='font-size: 16px; line-height: 1.6;'>
                        You requested to reset your password. Click the button below to create a new password:
                    </p>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetUrl}' style='background: #ffd700; color: #0a0a0a; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>
                            Reset Password
                        </a>
                    </div>

                    <p style='font-size: 14px; line-height: 1.6; color: #999;'>
                        Or copy and paste this link into your browser:
                    </p>
                    <p style='font-size: 14px; word-break: break-all; color: #ffd700;'>
                        {resetUrl}
                    </p>

                    <hr style='border: none; border-top: 1px solid #333; margin: 30px 0;'>

                    <p style='font-size: 14px; color: #999;'>
                        This link will expire in 1 hour for security reasons.
                    </p>

                    <p style='font-size: 14px; color: #999;'>
                        If you didn't request a password reset, please ignore this email. Your password will remain unchanged.
                    </p>

                    <div style='text-align: center; margin-top: 30px;'>
                        <p style='font-size: 12px; color: #666;'>
                            &copy; 2024 Mootable - Follow the White Rabbit
                        </p>
                    </div>
                </div>
            </body>
            </html>";

        await _emailService.SendEmailAsync(email, subject, body, isHtml: true);
    }
}