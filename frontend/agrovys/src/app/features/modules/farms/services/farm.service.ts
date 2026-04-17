import { Injectable } from '@angular/core';
import { BaseService } from '../../../../core/services/base.service';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { ListFarmsModel, FarmSimpleCreateModel } from '../models/farm.model';

@Injectable({
  providedIn: 'root',
})
export class FarmService extends BaseService {
  constructor(private http: HttpClient) {
    super();
  }

  getFarmsList(): Observable<ListFarmsModel[]> {
    const url = `${this.UrlServiceAgroVysApi}farms/`;
    return this.http.get<any[]>(url, this.GetAuthHeaderJson());
  }

  getFarmBoundaries(farmId: string): Observable<any[]> {
    const url = `${this.UrlServiceAgroVysApi}farms/${farmId}/boundaries`;
    return this.http.get<any[]>(url, this.GetAuthHeaderJson());
  }

  uploadBoundary(formData: FormData): Observable<any> {
    const url = `${this.UrlServiceAgroVysApi}farms/upload-boundary/`;
    return this.http.post(url, formData, this.GetAuthHeaderFormJson());
  }

  archiveFarms(farmIds: string[]): Observable<any> {
    const url = `${this.UrlServiceAgroVysApi}farms/archive/`;
    return this.http.patch(url, { farm_ids: farmIds }, this.GetAuthHeaderJson());
  }

  getAllBoundariesGeoJSON(): Observable<any> {
    const url = `${this.UrlServiceAgroVysApi}farms/boundaries/geojson`;
    return this.http.get<any>(url, this.GetAuthHeaderJson());
  }

  createSimpleFarm(farmData: FarmSimpleCreateModel): Observable<any> {
    const url = `${this.UrlServiceAgroVysApi}farms/simple/`;
    return this.http.post(url, farmData, this.GetAuthHeaderJson());
  }

  createFarmWithBoundaries(farmData: any, files: File[]): Observable<any> {
    const formData = new FormData();

    const url = `${this.UrlServiceAgroVysApi}farms/new-farm/`

    formData.append('name', farmData.name);
    formData.append('client_unit_id', farmData.clientUnitId);
    formData.append('agivys_user_id', farmData.agivysUserId);
    formData.append('crop_year', farmData.cropYear || '');

    files.forEach((file) => {
      formData.append('files', file, file.name);
    });

    return this.http.post(url, formData, this.GetAuthHeaderFormJson());
  }

  exportFarmsGeoJSON(farmIds: string[]): Observable<any> {
    const ids = farmIds.join(',');
    const url = `${this.UrlServiceAgroVysApi}farms/export/geojson?farm_ids=${ids}`;
    return this.http.get<any>(url, this.GetAuthHeaderJson());
  }

  exportFarmsKML(farmIds: string[]): Observable<any> {
    const ids = farmIds.join(',');
    const url = `${this.UrlServiceAgroVysApi}farms/export/kml?farm_ids=${ids}`;
    return this.http.get(url, { ...this.GetAuthHeaderJson(), responseType: 'text' as 'json' });
  }

}


