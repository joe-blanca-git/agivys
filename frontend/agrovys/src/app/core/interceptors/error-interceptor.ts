import {
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
import { LocalStorageUtils } from '../utils/localstorage';

export const errorInterceptor: HttpInterceptorFn = (
  req: HttpRequest<any>,
  next: HttpHandlerFn,
): Observable<HttpEvent<any>> => {
  const router = inject(Router);
  const toastService = inject(ToastService);
  const localStorageUtil = new LocalStorageUtils();

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
        if (err.status === 401) {
          toastService.error(err.error.message, 5000);
          localStorageUtil.clearLocaleUserData();
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
