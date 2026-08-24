import {
  APP_INITIALIZER,
  ApplicationConfig,
  provideZoneChangeDetection,
  importProvidersFrom,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { catchError, firstValueFrom, of } from 'rxjs';

import { routes } from './app.routes';
import { provideClientHydration } from '@angular/platform-browser';
import { pt_BR, provideNzI18n } from 'ng-zorro-antd/i18n';
import { registerLocaleData } from '@angular/common';
import pt from '@angular/common/locales/pt';
import { FormsModule } from '@angular/forms';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthGuardService } from './core/guards/auth.guard.ts.service';
import { AuthService } from './core/auth/auth.service';
import { ScreenService } from './core/services/screen.service';
import { errorInterceptor } from './core/interceptors/error-interceptor';
import { credentialsInterceptor } from './core/interceptors/credentials-interceptor';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';

registerLocaleData(pt);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideClientHydration(),
    provideNzI18n(pt_BR),
    importProvidersFrom(FormsModule),
    provideAnimationsAsync(),
    providePrimeNG({ 
        theme: {
            preset: Aura,
            options: {
                darkModeSelector: '.my-app-dark'
            }
        }
    }),
    provideHttpClient(withInterceptors([credentialsInterceptor, errorInterceptor])),
    ScreenService,
    AuthService,
    AuthGuardService,
    {
      // Confere no boot se o cookie de sessão (agivys_at) ainda é válido,
      // antes do router avaliar a primeira rota — sem isso, a guarda sempre
      // acha que ninguém está logado num F5 ou aba nova. Um 401 aqui é
      // esperado (usuário deslogado) e não deve travar o app.
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService) => () =>
        firstValueFrom(authService.checkSession().pipe(catchError(() => of(null)))),
      deps: [AuthService],
      multi: true,
    },
  ],
};
