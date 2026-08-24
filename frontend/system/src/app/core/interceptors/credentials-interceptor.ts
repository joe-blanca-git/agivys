import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environment/environment';

const AGIVYS_API_ORIGIN = new URL(environment.apiUrlAgiVys).origin;
const SAFE_METHODS = ['GET', 'HEAD', 'OPTIONS'];

function readCookie(name: string): string {
  if (typeof document === 'undefined') return '';

  const match = document.cookie.split('; ').find((row) => row.startsWith(`${name}=`));
  return match ? decodeURIComponent(match.split('=')[1]) : '';
}

/**
 * Faz o navegador enviar o cookie de sessão v2 (agivys_at) em toda chamada
 * pra API da AGIVYS, e ecoa o cookie CSRF legível (agivys_csrf) no header
 * X-CSRF-Token nas chamadas que alteram dado — sem isso o
 * RequireCsrfCookieMatchFilter do backend rejeita com 403.
 *
 * Só se aplica a requisições pra própria API da AGIVYS: BrasilAPI, ViaCEP e
 * qualquer outro host de terceiro seguem sem withCredentials, porque a
 * maioria das APIs públicas não aceita requisição com credenciais (CORS com
 * origin "*" e credentials juntos é rejeitado pelo navegador).
 */
export const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
  const targetsAgivysApi = req.url.startsWith(AGIVYS_API_ORIGIN);

  if (!targetsAgivysApi) {
    return next(req);
  }

  let authorizedReq = req.clone({ withCredentials: true });

  if (!SAFE_METHODS.includes(req.method)) {
    const csrfToken = readCookie('agivys_csrf');

    if (csrfToken) {
      authorizedReq = authorizedReq.clone({
        setHeaders: { 'X-CSRF-Token': csrfToken },
      });
    }
  }

  return next(authorizedReq);
};
