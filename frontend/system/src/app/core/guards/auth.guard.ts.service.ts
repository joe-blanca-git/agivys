import { Injectable } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
} from '@angular/router';
import { AuthService } from '../auth/auth.service';
const defaultPath = '/';

@Injectable()
export class AuthGuardService {
  constructor(private router: Router, private authService: AuthService) {}

  verifyPermitionRoute() {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): boolean {
    //verifica se usuario está logado
    const isLoggedIn = this.authService.loggedIn;

    //verifica se a rota é de configuração
    const isAuthForm = ['login', 'recovery-password', 'update-password', 'register'].includes(
      route.routeConfig?.path || defaultPath
    );

    // O JWT vive só num cookie HttpOnly — não há como validar a expiração aqui
    // no cliente. Se o cookie tiver expirado, a próxima chamada à API retorna
    // 401 e o errorInterceptor cuida de deslogar e redirecionar.

    // //se estiver logado e tentar acessar rotas de configuração, retorna para home
    if (isLoggedIn && isAuthForm) {
      this.authService._lastAuthenticatedPath = defaultPath;
      this.router.navigate([defaultPath]);
      return false;
    }

    //se nao estiver logado e nem estiver em rota de configuração, retorna para login
    if (!isLoggedIn && !isAuthForm) {
      this.router.navigate(['/auth/login']);
    }

    // // let routePath = state.url.replace('/', '');
    return isLoggedIn || isAuthForm;

  }
}
