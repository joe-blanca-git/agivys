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
  protected UrlServiceAgroVysApi: string = environment.apiUrlAgroVys;
  protected UrlServiceLoginV1: string = environment.apiUrlLoginv1;
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
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        Authorization: 'Bearer ' + this.LocalStorage.getUserToken(),
      }),
    };
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
