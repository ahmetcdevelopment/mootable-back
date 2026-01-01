namespace Mootable.Application.Pipelines.Logging;

/// <summary>
/// Kritik işlemlerin loglanması için marker interface.
/// 
/// NEDEN HER REQUEST LOGLANMIYOR:
/// High-frequency query'leri (GetMessages, GetOnlineUsers) loglamak
/// log storage'ı patlatır ve gerçek önemli logları gizler.
/// 
/// Sadece audit gerektirenler: Login, Register, CreateServer, DeleteMessage vb.
/// </summary>
public interface ILoggableRequest
{
    bool LogRequest => true;
    bool LogResponse => false;
}
