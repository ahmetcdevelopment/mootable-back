using Mootable.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mootable.Application.Interfaces;

namespace Mootable.Application.Features.Auth.Commands.ResetPassword;

/// <summary>
/// Handler for password reset
/// </summary>
public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IPasswordHasher passwordHasher,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ResetPasswordResponse> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find the reset token
            var resetToken = await _unitOfWork.Repository<PasswordResetToken>()
                .GetQueryableWithIncludes(t => t.User!)
                .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

            if (resetToken == null)
            {
                _logger.LogWarning("Invalid password reset token attempted: {Token}", request.Token);
                return new ResetPasswordResponse
                {
                    Success = false,
                    Message = "Invalid or expired reset token."
                };
            }

            // Check if token is valid
            if (!resetToken.IsValid())
            {
                _logger.LogWarning("Expired or used password reset token attempted: {TokenId}", resetToken.Id);
                return new ResetPasswordResponse
                {
                    Success = false,
                    Message = "This reset token has expired or has already been used."
                };
            }

            // Get the user
            var user = resetToken.User;
            if (user == null)
            {
                _logger.LogError("User not found for reset token: {TokenId}", resetToken.Id);
                return new ResetPasswordResponse
                {
                    Success = false,
                    Message = "Unable to reset password. Please try again."
                };
            }

            // Hash the new password
            var hashedPassword = _passwordHasher.Hash(request.NewPassword);
            user.PasswordHash = hashedPassword;
            user.UpdatedAt = DateTime.UtcNow;

            // Mark token as used
            resetToken.MarkAsUsed();

            // Invalidate all other unused reset tokens for this user
            var otherTokens = await _unitOfWork.Repository<PasswordResetToken>()
                .GetQueryable()
                .Where(t => t.UserId == user.Id && t.Id != resetToken.Id && !t.IsUsed)
                .ToListAsync(cancellationToken);

            foreach (var token in otherTokens)
            {
                token.MarkAsUsed();
            }

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send confirmation email
            await SendPasswordChangeConfirmationEmail(user.Email, user.DisplayName ?? user.Username);

            _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);

            return new ResetPasswordResponse
            {
                Success = true,
                Message = "Your password has been successfully reset. You can now log in with your new password."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password with token");
            return new ResetPasswordResponse
            {
                Success = false,
                Message = "An error occurred resetting your password. Please try again."
            };
        }
    }

    private async Task SendPasswordChangeConfirmationEmail(string email, string name)
    {
        var subject = "Your Mootable Password Has Been Changed";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; background: #0a0a0a; color: #e0e0e0; padding: 20px;'>
                <div style='max-width: 600px; margin: 0 auto; background: #1a1a1a; border-radius: 10px; padding: 30px; border: 1px solid #ffd700;'>
                    <h1 style='color: #ffd700; text-align: center; font-size: 28px; margin-bottom: 20px;'>
                        Password Changed Successfully
                    </h1>

                    <p style='font-size: 16px; line-height: 1.6;'>Hello {name},</p>

                    <p style='font-size: 16px; line-height: 1.6;'>
                        Your password has been successfully changed. You can now log in to your account with your new password.
                    </p>

                    <div style='background: #222; border-left: 4px solid #ffd700; padding: 15px; margin: 20px 0;'>
                        <p style='font-size: 14px; margin: 0; color: #ffd700;'>
                            <strong>Security Notice:</strong>
                        </p>
                        <p style='font-size: 14px; margin: 5px 0 0 0;'>
                            If you didn't make this change, please contact our support team immediately.
                        </p>
                    </div>

                    <p style='font-size: 14px; color: #999; margin-top: 30px;'>
                        For your security, we recommend:
                    </p>
                    <ul style='font-size: 14px; color: #999; line-height: 1.8;'>
                        <li>Using a unique password for each account</li>
                        <li>Enabling two-factor authentication</li>
                        <li>Never sharing your password with anyone</li>
                    </ul>

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