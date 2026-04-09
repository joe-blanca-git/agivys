import {
  Component,
  ElementRef,
  ViewChild,
  AfterViewInit,
  OnDestroy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';

@Component({
  selector: 'app-farm-preview-map',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="map-wrapper">
      <div #mapContainer class="map-frame"></div>
      
      <div class="info-card" *ngIf="shapeInfo">
        <h4 class="farm-title">{{ shapeInfo.nome }}</h4>
        <div class="info-row">
          <span class="info-label">Talhões:</span>
          <span class="info-value">{{ shapeInfo.talhoes }}</span>
        </div>
        <div class="info-row">
          <span class="info-label">Área Total:</span>
          <span class="info-value">{{ shapeInfo.area }} ha</span>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        width: 100%;
        height: 100%;
      }
      .map-wrapper {
        position: relative;
        width: 100%;
        height: 100%;
      }
      .map-frame {
        width: 100%;
        height: 100%;
        min-height: 400px;
        border-radius: 8px;
        overflow: hidden;
      }
      
      /* ESTILOS DO CARD FLUTUANTE */
      .info-card {
        position: absolute;
        bottom: 15px;
        left: 15px;
        z-index: 1000; 
        background-color: rgba(255, 255, 255, 0.66);
        backdrop-filter: blur(4px);
        padding: 10px 14px;
        border-radius: 6px;
        box-shadow: 0 4px 8px rgba(0,0,0,0.12);
        border-left: 3px solid #10b981;
        min-width: 180px;
      }
      .farm-title {
        margin: 0 0 8px 0;
        font-size: 13px;
        color: #333;
        font-weight: 700;
        text-transform: uppercase;
      }
      .info-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 4px; /* Deixa as linhas mais juntinhas */
        font-size: 11px; /* Fonte reduzida (ideal para painéis densos) */
      }
      .info-label {
        color: #666;
        font-weight: 500;
        margin-right: 12px; /* Garante que o texto e o valor não grudem */
      }
      .info-value {
        color: #111;
        font-weight: 600;
      }

      ::ng-deep .leaflet-control-layers {
        border-radius: 8px !important;
      }
    `,
  ],
})
export class FarmPreviewMapComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapContainer') mapContainer!: ElementRef;
  private map?: L.Map;
  private shapefileLayer?: L.GeoJSON;

  // Variável para armazenar as informações do card
  public shapeInfo: any = null;

  ngAfterViewInit(): void {
    this.initMap();
  }

  private initMap(): void {
    const googleRoads = L.tileLayer('https://{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}', { maxZoom: 20, subdomains: ['mt0', 'mt1', 'mt2', 'mt3'] });
    const googleSatellite = L.tileLayer('https://{s}.google.com/vt/lyrs=s,h&x={x}&y={y}&z={z}', { maxZoom: 20, subdomains: ['mt0', 'mt1', 'mt2', 'mt3'] });

    const savedLocation = localStorage.getItem('user_location');
    let coords: L.LatLngExpression = [-23.56183, -46.63777];
    if (savedLocation) {
      try {
        const parsed = JSON.parse(savedLocation);
        coords = [parsed.lat, parsed.lng];
      } catch (e) {}
    }

    this.map = L.map(this.mapContainer.nativeElement, {
      center: coords,
      zoom: 16,
      layers: [googleSatellite],
    });

    const baseMaps = { Mapa: googleRoads, Satélite: googleSatellite };
    L.control.layers(baseMaps, undefined, { position: 'topright' }).addTo(this.map);

    setTimeout(() => this.map?.invalidateSize(), 300);
  }

  // Atualizamos a função para receber a área calculada no Python
  public drawShapefile(geoJsonData: any, areaTotalHa: number): void {
    if (!this.map) return;

    if (this.shapefileLayer) {
      this.map.removeLayer(this.shapefileLayer);
    }

    this.shapefileLayer = L.geoJSON(geoJsonData, {
      style: {
        color: '#10b981',
        weight: 2,
        fillColor: '#10b981',
        fillOpacity: 0.3
      }
    }).addTo(this.map);

    // --- EXTRAINDO DADOS PARA O CARD ---
    // Pega o nome do primeiro polígono (como são da mesma fazenda)
    const primeiraFeature = geoJsonData.features[0];
    const nomeFazenda = primeiraFeature?.properties?.NOME_PROP || 'Fazenda Desconhecida';
    
    // Pega todos os códigos de talhão, remove os repetidos e junta com vírgula
    const talhoes = geoJsonData.features
      .map((f: any) => f.properties?.COD_TALHAO)
      .filter((value: any, index: any, self: any) => value && self.indexOf(value) === index)
      .join(', ');

    // Preenche a variável que faz o card aparecer na tela!
    this.shapeInfo = {
      nome: nomeFazenda,
      talhoes: talhoes || 'N/A',
      area: areaTotalHa // A área que veio do Python!
    };
    // -----------------------------------

    const bounds = this.shapefileLayer.getBounds();
    this.map.fitBounds(bounds, { padding: [30, 30] });
  }

  ngOnDestroy(): void {
    if (this.map) this.map.remove();
  }
}