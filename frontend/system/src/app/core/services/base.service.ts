import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environment/environment';
import { LocalStorageUtils } from '../utils/localstorage';

@Injectable({
  providedIn: 'root',
})

export abstract class BaseService {

  constructor() {}

  public LocalStorage = new LocalStorageUtils();
  protected UrlServiceApi: string = environment.apiUrlAgiVys;
  protected UrlServiceLoginV1: string = environment.apiUrlLoginv1;
  protected UrlServiceAuthV2: string = environment.apiUrlAuthV2;
  protected UrlServiceBrApi: string = environment.apiUrlBrApi;
  protected UrlServiceViaCep: string = environment.apiRulViaCep;

  protected GetHeaderJson() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      }),
    };
  }

  protected GetAuthHeaderJson() {
    const token = this.LocalStorage.getUserToken();

    return {
      headers: new HttpHeaders(
        token
          ? { 'Content-Type': 'application/json', Authorization: 'Bearer ' + token }
          : { 'Content-Type': 'application/json' },
      ),
    };
  }

  /**
   * Pro login v2: precisa de withCredentials pro navegador aceitar e guardar
   * os cookies Set-Cookie da resposta (não existe CSRF ainda nesse ponto).
   */
  protected GetCredentialsHeaderJson() {
    return {
      withCredentials: true,
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      }),
    };
  }

  /**
   * Pra chamadas autenticadas pela sessão v2 (cookie): manda o cookie
   * automaticamente (withCredentials) e ecoa o cookie CSRF legível no header
   * X-CSRF-Token, exigido pelo RequireCsrfCookieMatchFilter no backend.
   */
  protected GetCsrfHeaderJson() {
    return {
      withCredentials: true,
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        'X-CSRF-Token': this.getCsrfCookie(),
      }),
    };
  }

  private getCsrfCookie(): string {
    if (typeof document === 'undefined') return '';

    const match = document.cookie
      .split('; ')
      .find((row) => row.startsWith('agivys_csrf='));

    return match ? decodeURIComponent(match.split('=')[1]) : '';
  }

  protected GetAuthHeaderTokenJson(token: string) {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        Authorization: 'Bearer ' + token,
      }),
    };
  }

  protected GetHeaderUnlercoded() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/x-www-form-urlencoded',
      }),
    };
  }

  protected extractData(response: any) {

    return response || {};
  }

}
