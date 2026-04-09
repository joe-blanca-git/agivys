import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { BaseService } from '../../../../../../core/services/base.service';

@Injectable({
  providedIn: 'root',
})
export class AccountService extends BaseService {
  constructor(private http: HttpClient) {
    super();
  }

  registerAddress(address: any): Observable<any> {
    const url = `${this.UrlServiceLoginV1}my-addresses`;

    return this.http
      .post(url, address, this.GetAuthHeaderJson())
      .pipe(map(this.extractData));
  }

  registerCompany(company: any): Observable<any> {
    const url = `${this.UrlServiceApi}companies`;

    return this.http
      .post(url, company, this.GetAuthHeaderJson())
      .pipe(map(this.extractData));
  }

  registerCompanyAddress(companyAddress: any): Observable<any> {
    const url = `${this.UrlServiceLoginV1}addresses`;

    return this.http
      .post(url, companyAddress, this.GetAuthHeaderJson())
      .pipe(map(this.extractData));
  }
}
