import { Injectable } from '@angular/core';
import { BaseService } from '../../../../core/services/base.service';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class FarmService extends BaseService {
  constructor(private http: HttpClient) {
    super();
  }

  uploadBoundary(formData: FormData): Observable<any> {
    const url = `${this.UrlServiceAgroVysApi}farms/upload-boundary/`;
    return this.http.post(url, formData, this.GetAuthHeaderFormJson());
  }

  createFarmWithBoundaries(farmData: any, files: File[]): Observable<any> {
    const formData = new FormData();

    const url = `${this.UrlServiceAgroVysApi}farms/new-farm/`

    formData.append('name', farmData.name);
    formData.append('client_unit_id', farmData.clientUnitId);
    formData.append('agivys_user_id', farmData.agivysUserId);
    formData.append('crop_year', farmData.cropYear || '2024/2025');

    files.forEach((file) => {
      formData.append('files', file, file.name); 
    });

    return this.http.post(url, formData);
  }

}
