using Mootable.Application.Features.RabbitHoles.Constants;
using Mootable.Domain.Entities;
using Mootable.Domain.Exceptions;

namespace Mootable.Application.Features.RabbitHoles.Rules;

/// <summary>
/// RabbitHole domain business rules.
/// 
/// PRODUCTION DENEYİMİ (THREAD SİSTEMLERİ):
/// Slack/Discord thread sistemlerinde en sık görülen bug:
/// - Aynı mesajdan birden fazla thread açılması
/// - Kilitli thread'e mesaj gönderilmesi (race condition)
/// - Silinen mesajın thread'inin orphan kalması
/// 
/// Bu rules class'ı bu edge case'leri merkezi kontrol eder.
/// </summary>
public sealed class RabbitHoleBusinessRules
{
    public void RabbitHoleMustExist(RabbitHole? rabbitHole)
    {
        if (rabbitHole == null || rabbitHole.IsDeleted)
        {
            throw new BusinessRuleException("RH_001", RabbitHoleMessages.RabbitHoleNotFound);
        }
    }

    public void RabbitHoleMustNotBeLocked(RabbitHole rabbitHole)
    {
        if (rabbitHole.IsLocked)
        {
            throw new BusinessRuleException("RH_002", RabbitHoleMessages.RabbitHoleLocked);
        }
    }

    public void MessageMustExist(Message? message)
    {
        if (message == null || message.IsDeleted)
        {
            throw new BusinessRuleException("RH_003", RabbitHoleMessages.MessageNotFound);
        }
    }

    public void MessageMustNotHaveRabbitHole(bool hasRabbitHole)
    {
        if (hasRabbitHole)
        {
            throw new BusinessRuleException("RH_004", RabbitHoleMessages.MessageAlreadyHasRabbitHole);
        }
    }
}
