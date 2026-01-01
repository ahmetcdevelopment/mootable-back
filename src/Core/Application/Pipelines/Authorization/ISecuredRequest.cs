namespace Mootable.Application.Pipelines.Authorization;

/// <summary>
/// Bu interface'i implement eden her request, AuthorizationBehavior tarafından otomatik kontrol edilir.
/// Controller seviyesinde [Authorize] attribute kullanılmaz - authorization tamamen pipeline'da yapılır.
/// 
/// ANTI-PATTERN UYARISI:
/// Controller'da [Authorize(Roles = "Admin")] kullanmak, authorization mantığını dağıtır.
/// 6 ay sonra "bu endpoint'e kim erişebilir?" sorusuna cevap vermek için 50+ controller taramak gerekir.
/// Bu yapıda tek bakılacak yer: request class'ının Roles property'si.
/// </summary>
public interface ISecuredRequest
{
    string[] Roles { get; }
}
