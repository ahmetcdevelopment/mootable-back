using Mootable.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mootable.Application.Interfaces;

namespace Mootable.Application.Features.Auth.Commands.DeleteAccount;

/// <summary>
/// Handler for account deletion
/// </summary>
public sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, DeleteAccountResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ILogger<DeleteAccountCommandHandler> _logger;

    public DeleteAccountCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        ILogger<DeleteAccountCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<DeleteAccountResponse> Handle(
        DeleteAccountCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user
            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .Include(u => u.UserRoles)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return new DeleteAccountResponse
                {
                    Success = false,
                    Message = "User account not found."
                };
            }

            // Verify password
            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed account deletion attempt for user {UserId} - invalid password", user.Id);
                return new DeleteAccountResponse
                {
                    Success = false,
                    Message = "Invalid password. Account deletion cancelled."
                };
            }

            // Verify confirmation text
            if (request.ConfirmationText != "DELETE MY ACCOUNT")
            {
                return new DeleteAccountResponse
                {
                    Success = false,
                    Message = "Invalid confirmation text. Please type 'DELETE MY ACCOUNT' exactly."
                };
            }

            // Store email for notification before deletion
            var userEmail = user.Email;
            var userName = user.DisplayName ?? user.Username;

            // Begin transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Delete related data
                // 1. Delete user roles
                var userRoles = await _unitOfWork.Repository<UserRole>()
                    .GetQueryable()
                    .Where(ur => ur.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                foreach (var userRole in userRoles)
                {
                    _unitOfWork.Repository<UserRole>().Delete(userRole);
                }

                // 2. Delete refresh tokens
                var refreshTokens = await _unitOfWork.Repository<Mootable.Domain.Entities.RefreshToken>()
                    .GetQueryable()
                    .Where(rt => rt.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                foreach (var token in refreshTokens)
                {
                    _unitOfWork.Repository<Mootable.Domain.Entities.RefreshToken>().Delete(token);
                }

                // 3. Delete external logins
                var externalLogins = await _unitOfWork.Repository<Mootable.Domain.Entities.ExternalLogin>()
                    .GetQueryable()
                    .Where(el => el.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                foreach (var externalLogin in externalLogins)
                {
                    _unitOfWork.Repository<Mootable.Domain.Entities.ExternalLogin>().Delete(externalLogin);
                }

                // 4. Delete password reset tokens
                var passwordResetTokens = await _unitOfWork.Repository<PasswordResetToken>()
                    .GetQueryable()
                    .Where(prt => prt.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                foreach (var resetToken in passwordResetTokens)
                {
                    _unitOfWork.Repository<PasswordResetToken>().Delete(resetToken);
                }

                // 5. Anonymize messages (don't delete them to preserve conversation history)
                var messages = await _unitOfWork.Repository<Message>()
                    .GetQueryable()
                    .Where(m => m.AuthorId == user.Id)
                    .ToListAsync(cancellationToken);

                foreach (var message in messages)
                {
                    message.AuthorId = Guid.Empty; // Set to a null/deleted user ID
                    message.UpdatedAt = DateTime.UtcNow;
                }

                // 6. Delete server memberships
                var serverMembers = await _unitOfWork.Repository<ServerMember>()
                    .GetQueryable()
                    .Where(sm => sm.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                foreach (var serverMember in serverMembers)
                {
                    _unitOfWork.Repository<ServerMember>().Delete(serverMember);
                }

                // 7. Finally, delete the user
                _unitOfWork.Repository<User>().Delete(user);

                // Commit transaction
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Send farewell email
                await SendAccountDeletionEmail(userEmail, userName);

                _logger.LogInformation("User account {UserId} successfully deleted", user.Id);

                return new DeleteAccountResponse
                {
                    Success = true,
                    Message = "Your account has been permanently deleted. We're sorry to see you go."
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error during account deletion for user {UserId}", user.Id);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing account deletion for user {UserId}", request.UserId);
            return new DeleteAccountResponse
            {
                Success = false,
                Message = "An error occurred while deleting your account. Please try again or contact support."
            };
        }
    }

    private async Task SendAccountDeletionEmail(string email, string name)
    {
        var subject = "Your Mootable Account Has Been Deleted";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; background: #0a0a0a; color: #e0e0e0; padding: 20px;'>
                <div style='max-width: 600px; margin: 0 auto; background: #1a1a1a; border-radius: 10px; padding: 30px; border: 1px solid #ffd700;'>
                    <h1 style='color: #ffd700; text-align: center; font-size: 28px; margin-bottom: 20px;'>
                        Account Deleted
                    </h1>

                    <p style='font-size: 16px; line-height: 1.6;'>Dear {name},</p>

                    <p style='font-size: 16px; line-height: 1.6;'>
                        Your Mootable account has been permanently deleted as requested.
                    </p>

                    <div style='background: #222; border-left: 4px solid #ffd700; padding: 15px; margin: 20px 0;'>
                        <p style='font-size: 14px; margin: 0;'>
                            <strong>What happens next:</strong>
                        </p>
                        <ul style='font-size: 14px; margin: 10px 0 0 20px;'>
                            <li>All your personal data has been removed</li>
                            <li>Your messages have been anonymized</li>
                            <li>This action cannot be undone</li>
                        </ul>
                    </div>

                    <p style='font-size: 16px; line-height: 1.6;'>
                        We're sorry to see you go. If you ever decide to return, you're always welcome to create a new account.
                    </p>

                    <p style='font-size: 14px; color: #999; margin-top: 30px;'>
                        If you didn't request this deletion, please contact our support team immediately at support@mootable.com
                    </p>

                    <hr style='border: none; border-top: 1px solid #333; margin: 30px 0;'>

                    <div style='text-align: center;'>
                        <p style='font-size: 14px; color: #666; font-style: italic;'>
                            &quot;What we know, we know from the Matrix.&quot;
                        </p>
                        <p style='font-size: 12px; color: #666; margin-top: 20px;'>
                            &copy; 2024 Mootable - Until we meet again
                        </p>
                    </div>
                </div>
            </body>
            </html>";

        await _emailService.SendEmailAsync(email, subject, body, isHtml: true);
    }
}