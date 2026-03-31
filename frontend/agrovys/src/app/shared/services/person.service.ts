import { Injectable } from '@angular/core';
import { BaseService } from '../../core/services/base.service';
import { HttpClient } from '@angular/common/http';
import { PersonModel } from '../models/person.model';
import { Observable, catchError, map } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class PersonService extends BaseService {
  constructor(private httpClient: HttpClient) {
    super();
  }

  getPersonList(typePerson: string): Observable<PersonModel[]> {
    let url = `${this.UrlServiceApi}Person/list`;

    if (typePerson) {
      url = `${url}?type=${typePerson}`;
    }

    return this.httpClient
      .get<any>(url, this.GetAuthHeaderJson())
      .pipe(
        map((response) => this.extractData(response)),
        catchError((error) => {
          throw error;
        })
      );
  }

  createPerson(person: any): Observable<any> {
    const url = `${this.UrlServiceLoginV1}register-full`;

    return this.httpClient
      .post<any>(url, person, this.GetAuthHeaderJson())
      .pipe(
        map((response) => this.extractData(response)),
        catchError((error) => {
          throw error;
        })
      );
  }
}