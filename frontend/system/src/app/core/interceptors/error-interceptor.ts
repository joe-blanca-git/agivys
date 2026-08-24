import {
  HttpContextToken,
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastService } from '../services/toast.service';
import { AuthService } from '../auth/auth.service';

/**
 * Pra requisições onde um 401 é um resultado normal (ex.: checar se a sessão
 * ainda existe no boot do app) — evita o toast de erro e o redirect pro
 * login que esse interceptor dispara por padrão em qualquer 401.
 */
export const SKIP_AUTH_REDIRECT = new HttpContextToken<boolean>(() => false);

export const errorInterceptor: HttpInterceptorFn = (
  req: HttpRequest<any>,
  next: HttpHandlerFn,
): Observable<HttpEvent<any>> => {
  const router = inject(Router);
  const toastService = inject(ToastService);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((err: any) => {

      if (err instanceof HttpErrorResponse) {
        if (err.status === 400) {
          //erro de senha menor ou maior que o permitido.
          if (err.error.errors.Senha) {
            toastService.error(err.error.message, 5000);
          }

          //outros erros
          if (err.error.errors.Mensagens) {
            toastService.error(err.error.message, 5000);
          }
        }

        //erro de autenticação
        if (err.status === 401 && !req.context.get(SKIP_AUTH_REDIRECT)) {
          toastService.error(err.error?.message ?? 'Sessão expirada, faça login novamente.', 5000);
          authService.clearSession();
          router.navigate(['/auth/login']);
        }

        //erro: Proibido (Usuário conhecido, mas sem permissão)
        if (err.status === 403) {
          toastService.error(err.error.message, 5000);
          router.navigate(['/access-denied']);
        }

        if (err.status === 500) {
          toastService.error('Erro interno, tente novamente mais tarde!', 5000);
        }
      }

      return throwError(() => err);
    }),
  );
};
