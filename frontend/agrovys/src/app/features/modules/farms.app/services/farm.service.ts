import { Injectable } from '@angular/core';
import { BaseService } from '../../../../core/services/base.service';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class FarmService extends BaseService {

  constructor(private http: HttpClient) {
    super()
  }

  uploadBoundary(formData: FormData): Observable<any> {
    const url = `${this.UrlServiceAgroVysApi}upload-boundary/`
    return this.http.post(url, formData);
  }
}
