import { Injectable } from '@angular/core';
import { BaseService } from '../../core/services/base.service';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError } from 'rxjs';
import { AccountModel } from '../models/account.model';

@Injectable({
  providedIn: 'root',
})
export class AccountService extends BaseService {
  constructor(private httpClient: HttpClient) {
    super();
  }

  getAccountList(): Observable<AccountModel[]> {
    let url = `${this.UrlServiceApi}FinancialAccount`;

    return this.httpClient.get<any>(url, this.GetAuthHeaderJson()).pipe(
      map((response) => this.extractData(response)),
      catchError((error) => {
        throw error;
      }),
    );
  }


}
