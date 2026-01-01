namespace Mootable.Application.Pipelines.Transaction;

/// <summary>
/// Transaction gerektiren Command'ler için marker interface.
/// 
/// NEDEN HER COMMAND TRANSACTION İÇİNDE DEĞİL:
/// Read-only query'ler ve tek entity update'leri için transaction overhead gereksiz.
/// Sadece multi-entity mutation'lar için kullanılmalı.
/// 
/// Örnek kullanım senaryoları:
/// - CreateServer (Server + ServerMember + ServerRole oluşturma)
/// - SendMessage (Message + MessageAttachment + RabbitHole check)
/// </summary>
public interface ITransactionalRequest
{
}
