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

    // //verifica se token é válido
    if (isLoggedIn && !this.authService.isTokenValid()) {
      this.authService.logOut();
      this.router.navigate(['/auth/login']);
      return false;
    }

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
