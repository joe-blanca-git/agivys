import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BaseService } from '../../../../core/services/base.service';
import { catchError, map, Observable } from 'rxjs';
import {
  FinancialCategoryModel,
  FinancialMovModel,
  FinancialOriginModel,
  PaymentSettle,
} from '../models/financial.model';

export interface IBodyFinancialCategory {
  name: string;
  type: number;
}

export interface IBodyFinancialOrigin {
  description: string;
}

export interface IBodyFinancialMov {
  description: string;
  type: number;
  totalAmount: number;
  documentDate: string;
  installmentsCount: number;
  firstDueDate: string;
  categoryId: string;
  accountId: string;
  originId: string;
  personId: string;
}

export interface IBodyPaymentSettle {
  transactionId: string;
  accountId: string;
  totalAmountPaid: number;
  paymentDate: string;
  paymentMethod: string;
}

@Injectable({
  providedIn: 'root',
})
export class FinancialService extends BaseService {
  constructor(private httpClient: HttpClient) {
    super();
  }

  //gets
  getFinancialMov(filters?: {
    Q?: string;
    StartDate?: string;
    EndDate?: string;
    PersonId?: string;
    CategoryId?: string;
    OriginId?: string;
    AccountId?: string;
    Type?: number;
    Page?: number;
    PageSize?: number;
  }): Observable<FinancialMovModel[]> {
    const url = `${this.UrlServiceApi}FinancialTransaction`;

    let params = new HttpParams();

    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== null && value !== undefined && value !== '') {
          params = params.set(key, value.toString());
        }
      });
    }

    return this.httpClient
      .get<any>(url, {
        ...this.GetAuthHeaderJson(),
        params,
      })
      .pipe(
        map((response) => this.extractData(response)),
        catchError((error) => {
          throw error;
        }),
      );
  }

  getFinancialOrign(): Observable<FinancialOriginModel[]> {
    const url = `${this.UrlServiceApi}FinancialOrigin`;

    return this.httpClient.get<any>(url, this.GetAuthHeaderJson()).pipe(
      map((response) => this.extractData(response)),
      catchError((error) => {
        throw error;
      }),
    );
  }

  getFinancialCategory(): Observable<FinancialCategoryModel[]> {
    const url = `${this.UrlServiceApi}FinancialCategory`;

    return this.httpClient.get<any>(url, this.GetAuthHeaderJson()).pipe(
      map((response) => this.extractData(response)),
      catchError((error) => {
        throw error;
      }),
    );
  }

  

  //posts
  postFinancialOrigin(
    origin: IBodyFinancialOrigin,
  ): Observable<FinancialOriginModel> {
    const url = `${this.UrlServiceApi}FinancialOrigin`;

    return this.httpClient
      .post<any>(url, origin, this.GetAuthHeaderJson())
      .pipe(
        map((response) => this.extractData(response)),
        catchError((error) => {
          throw error;
        }),
      );
  }

  postFinancialCategpry(
    categpry: IBodyFinancialCategory,
  ): Observable<FinancialCategoryModel> {
    const url = `${this.UrlServiceApi}FinancialCategory`;

    return this.httpClient
      .post<any>(url, categpry, this.GetAuthHeaderJson())
      .pipe(
        map((response) => this.extractData(response)),
        catchError((error) => {
          throw error;
        }),
      );
  }

  postFinancialMov(mov: IBodyFinancialMov): Observable<FinancialMovModel> {
    const url = `${this.UrlServiceApi}FinancialTransaction`;

    return this.httpClient.post<any>(url, mov, this.GetAuthHeaderJson()).pipe(
      map((response) => this.extractData(response)),
      catchError((error) => {
        throw error;
      }),
    );
  }

  postPaymentSettle(payment: IBodyPaymentSettle): Observable<PaymentSettle> {
    const url = `${this.UrlServiceApi}FinancialPayment/settle-transaction`;

    return this.httpClient
      .post<any>(url, payment, this.GetAuthHeaderJson())
      .pipe(
        map((response) => this.extractData(response)),
        catchError((error) => {
          throw error;
        }),
      );
  }
}
