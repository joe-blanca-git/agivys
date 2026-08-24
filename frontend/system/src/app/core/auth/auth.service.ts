import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { LocalStorageUtils } from '../utils/localstorage';
import { lastValueFrom, map, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { BaseService } from '../services/base.service';
import { SKIP_AUTH_REDIRECT } from '../interceptors/error-interceptor';

export interface IUser {
  email: string;
  name?: string;
  avatarUrl?: string;
}

const defaultPath = '/';

@Injectable({
  providedIn: 'root',
})
export class AuthService extends BaseService {
  localStorageUtils = new LocalStorageUtils();

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {
    super();
  }

  // O JWT vive só num cookie HttpOnly (nunca chega no JS). Esse flag e o
  // perfil abaixo ficam só em memória — não sobrevivem a um F5, e é assim
  // de propósito: não há mais nada de sessão pra ler do localStorage.
  private _isLoggedIn = false;
  private _user: IUser | null = null;
  public _lastAuthenticatedPath: string = defaultPath;

  get loggedIn(): boolean {
    return this._isLoggedIn;
  }

  get currentUser(): IUser | null {
    return this._user;
  }

  login(email: string, password: string): Observable<any> {
    const url = `${this.UrlServiceAuthV2}login`;

    return this.http
      .post(url, { email, password }, this.GetCredentialsHeaderJson())
      .pipe(
        map(this.extractData),
        tap((response: any) => {
          this._isLoggedIn = true;
          this._user = {
            email: response?.user?.email,
            name: response?.person?.name,
          };
        }),
      );
  }

  async logOut() {
    const url = `${this.UrlServiceAuthV2}logout`;

    try {
      // O cookie é HttpOnly: só a própria API consegue apagá-lo.
      await lastValueFrom(this.http.post(url, {}, this.GetCsrfHeaderJson()));
    } catch (error) {
      console.error(error);
    }

    // Limpa qualquer resquício de sessões salvas por versões anteriores do app.
    this.localStorageUtils.clearLocaleUserData();

    this.clearSession();
    await this.router.navigate(['/auth']);
  }

  clearSession(): void {
    this._isLoggedIn = false;
    this._user = null;
  }

  /**
   * Pergunta pra API se o cookie agivys_at ainda vale e, se sim, repopula o
   * perfil em memória. Chamada uma vez no boot do app (ver APP_INITIALIZER
   * em app.config.ts) — é o que permite continuar logado num F5/aba nova
   * sem guardar nada no cliente: quem sabe se a sessão existe é o servidor.
   */
  checkSession(): Observable<any> {
    const url = `${this.UrlServiceAuthV2}me`;

    return this.http
      .get(url, {
        ...this.GetCredentialsHeaderJson(),
        context: new HttpContext().set(SKIP_AUTH_REDIRECT, true),
      })
      .pipe(
        map(this.extractData),
        tap((response: any) => {
          this._isLoggedIn = true;
          this._user = {
            email: response?.user?.email,
            name: response?.person?.name,
          };
        }),
      );
  }

  register(user: any): Observable<any> {
    const url = `${this.UrlServiceLoginV1}register`;

    return this.http
      .post(url, user, this.GetHeaderJson())
      .pipe(map(this.extractData));
  }

  forgotPassword(email: string): Observable<any> {
    const url = `${this.UrlServiceLoginV1}forgot-password`;

    return this.http
      .post(url, { email }, this.GetHeaderJson())
      .pipe(map(this.extractData));
  }

  //criar chamada de api que faz somente o endpoint register

  verifyExitingEmail(email: string): Observable<{ exists: boolean }> {
    const url = `${this.UrlServiceLoginV1}check-email/${email}`;
    console.log('s');

    return this.http.get<{ exists: boolean }>(url, this.GetAuthHeaderJson());
  }

  verifyExitingDocument(document: string): Observable<{ exists: boolean }> {
    const url = `${this.UrlServiceLoginV1}check-cpf/${document}`;
    console.log('s');

    return this.http.get<{ exists: boolean }>(url, this.GetAuthHeaderJson());
  }
}
