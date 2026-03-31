import { Injectable } from '@angular/core';
import { BaseService } from '../../core/services/base.service';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { CitiesPublicModel } from '../models/address.model';

@Injectable({
  providedIn: 'root',
})
export class AddressVerifyService extends BaseService {
  constructor(private http: HttpClient) {
    super();
  }

  getZipCode(cep: string): Observable<{ exists: boolean }> {
    const url = `${this.UrlServiceViaCep}${cep}/json/`;
    return this.http.get<{ exists: boolean }>(url, this.GetAuthHeaderJson());
  }

  getCities(state: string): Observable<CitiesPublicModel[]> {
    const url = `${this.UrlServiceBrApi}ibge/municipios/v1/${state}?providers=dados-abertos-br,gov,wikipedia`;
    return this.http.get<CitiesPublicModel[]>(url, this.GetAuthHeaderJson());
  }

  getState(): Observable<CitiesPublicModel[]> {
    const url = `${this.UrlServiceBrApi}ibge/uf/v1`;
    return this.http.get<CitiesPublicModel[]>(url, this.GetAuthHeaderJson());
  }
}
