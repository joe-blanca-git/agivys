import { Injectable } from '@angular/core';
import { BaseService } from '../../../../core/services/base.service';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { ClientUnitModel } from '../models/client.model';

@Injectable({
  providedIn: 'root'
})
export class ClientService extends BaseService {

  constructor(private http: HttpClient) {
    super()
  }

  createClientUnit(clientData: ClientUnitModel): Observable<any> {
    const formData = new FormData();

    formData.append('name', clientData.name);
    formData.append('agivys_user_id', clientData.agivysUserId);
    formData.append('agivys_company_id', clientData.agivysCompanyId);

    const url = `${this.UrlServiceAgroVysApi}clients`

    return this.http.post(url, formData, this.GetAuthHeaderFormJson());
  }

  getClients(): Observable<ClientUnitModel[]> {
    const url = `${this.UrlServiceAgroVysApi}clients`
    return this.http.get<ClientUnitModel[]>(url, this.GetAuthHeaderJson());
  }


}
