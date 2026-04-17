import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin, Observable, tap } from 'rxjs';
import { FarmService } from '../../services/farm.service';

@Component({
  selector: 'app-farm-export',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './farm-export.component.html',
  styleUrl: './farm-export.component.scss'
})
export class FarmExportComponent {
  @Input() selectedFarms: any[] = [];
  @Output() statusExport = new EventEmitter<boolean>();

  selectedExportType: string | null = null;
  isSeparateFiles: boolean = false;
  isLoadingExport: boolean = false;

  constructor(private farmService: FarmService) { }

  selectType(type: string) {
    this.selectedExportType = type;
    this.emitStatus();
    console.log('Tipo de exportação selecionado:', type);
  }

  emitStatus() {
    this.statusExport.emit(this.getStatusForm());
  }

  getStatusForm(): boolean {
    return !!this.selectedExportType && !this.isLoadingExport;
  }

  exportFarms() {
    if (!this.selectedExportType) return;

    const farmIds = this.selectedFarms.map(f => f.id);
    const downloads: Observable<any>[] = [];

    this.isLoadingExport = true;
    this.emitStatus();

    if (this.isSeparateFiles) {
      farmIds.forEach(id => {
        if (this.selectedExportType === 'geojson') downloads.push(this.getGeoJSONObservable([id]));
        if (this.selectedExportType === 'kml') downloads.push(this.getKMLObservable([id]));
      });
    } else {
      if (this.selectedExportType === 'geojson') downloads.push(this.getGeoJSONObservable(farmIds));
      if (this.selectedExportType === 'kml') downloads.push(this.getKMLObservable(farmIds));
    }

    if (downloads.length > 0) {
      forkJoin(downloads).pipe(
        finalize(() => {
          this.isLoadingExport = false;
          this.emitStatus();
        })
      ).subscribe({
        error: (err) => console.error('Erro na exportação:', err)
      });
    } else {
      this.isLoadingExport = false;
      this.emitStatus();
    }
  }

  private getGeoJSONObservable(ids: string[]): Observable<any> {
    return this.farmService.exportFarmsGeoJSON(ids).pipe(
      tap((data) => {
        const blob = new Blob([JSON.stringify(data)], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = ids.length === 1 ? `fazenda_${ids[0]}.geojson` : 'fazendas_export.geojson';
        link.click();
        window.URL.revokeObjectURL(url);
      })
    );
  }

  private getKMLObservable(ids: string[]): Observable<any> {
    return this.farmService.exportFarmsKML(ids).pipe(
      tap((data) => {
        const blob = new Blob([data], { type: 'application/vnd.google-earth.kml+xml' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = ids.length === 1 ? `fazenda_${ids[0]}.kml` : 'fazendas_export.kml';
        link.click();
        window.URL.revokeObjectURL(url);
      })
    );
  }
}
