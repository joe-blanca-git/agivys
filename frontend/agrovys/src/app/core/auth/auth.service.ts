import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { LocalStorageUtils } from '../utils/localstorage';
import { map, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { BaseService } from '../services/base.service';

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

  private _user: IUser | null = null;
  public _lastAuthenticatedPath: string = defaultPath;

  get loggedIn(): boolean {
    const loggedUser = this.localStorageUtils.getUser();

    if (loggedUser) {
      const email = loggedUser.email;
      const name = loggedUser.nome;
      this._user = { email, name };
    } else {
      this._user = null;
    }

    return !!this._user;
  }

  login(email: string, password: string): Observable<string> {
    const body = `{
      "email" : "${email}",
      "password": "${password}"
    }`;

    const url = `${this.UrlServiceLoginV1}login`;

    const response = this.http
      .post(url, body, this.GetHeaderJson())
      .pipe(map(this.extractData));

    return response;
  }

  async logOut() {
    this.localStorageUtils.clearLocaleUserData();
    this._user = null;
    await this.router.navigate(['/auth']);
  }

  register(user: any): Observable<any> {
    const url = `${this.UrlServiceLoginV1}register`;

    return this.http
      .post(url, user, this.GetHeaderJson())
      .pipe(map(this.extractData));
  }

  isTokenValid(): boolean {
    const token = this.localStorageUtils.getUserToken();
    if (!token) return false;

    const parts = token.split('.');
    if (parts.length !== 3) return false;

    try {
      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const payload = JSON.parse(
        decodeURIComponent(
          atob(base64)
            .split('')
            .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
            .join(''),
        ),
      );

      const now = Date.now();
      const clockToleranceMs = 5000;

      if (!payload.exp) return false;

      const exp = payload.exp * 1000;
      const nbf = payload.nbf ? payload.nbf * 1000 : null;
      if (nbf && now < (nbf - clockToleranceMs)) return false;

      if (now >= (exp + clockToleranceMs)) return false;

      return true;
    } catch {
      return false;
    }
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
