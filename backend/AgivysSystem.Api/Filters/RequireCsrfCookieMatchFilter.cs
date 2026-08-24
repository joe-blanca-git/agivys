using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgiVysSystem.Api.Filters;

/// <summary>
/// Proteção contra CSRF para a sessão baseada em cookie (auth v2).
///
/// Dupla submissão: no login, a API grava um cookie legível (<c>agivys_csrf</c>)
/// com um valor aleatório. Um site de fora não consegue ler esse cookie (é de
/// outro domínio) nem adivinhar o valor, então só quem está de fato na página
/// consegue ecoá-lo no header <c>X-CSRF-Token</c>.
///
/// Só se aplica quando a requisição está autenticada pelo cookie <c>agivys_at</c>
/// (sem header Authorization) — clientes v1 que mandam Bearer no header (portal-pat,
/// outros sistemas) não sofrem CSRF da mesma forma e passam direto, sem essa checagem.
/// </summary>
public class RequireCsrfCookieMatchFilter : IActionFilter
{
    private const string AccessTokenCookie = "agivys_at";
    private const string CsrfCookie = "agivys_csrf";
    private const string CsrfHeader = "X-CSRF-Token";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;

        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
            return;

        // Veio Bearer no header: é um cliente v1 (ou qualquer client fora do navegador).
        // CSRF não se aplica a esse caminho — segue sem checar.
        if (!string.IsNullOrEmpty(request.Headers.Authorization))
            return;

        // Sem header e sem cookie de sessão v2: não há sessão por cookie pra proteger aqui.
        if (!request.Cookies.ContainsKey(AccessTokenCookie))
            return;

        var cookieValue = request.Cookies[CsrfCookie];
        var headerValue = request.Headers[CsrfHeader].ToString();

        if (string.IsNullOrEmpty(cookieValue) || cookieValue != headerValue)
        {
            context.Result = new ObjectResult(new { message = "Token CSRF ausente ou inválido." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
