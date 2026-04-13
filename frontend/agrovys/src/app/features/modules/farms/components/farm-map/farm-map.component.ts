import { Component, ElementRef, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';

@Component({
  selector: 'app-farm-map',
  standalone: true,
  imports: [CommonModule],
  template: `<div #mapContainer class="map-frame"></div>`,
  styles: [`
    :host { display: block; width: 100%; height: 100%; }
    .map-frame { width: 100%; height: 100%; min-height: 400px; }
    
    ::ng-deep .leaflet-control-layers {
      border-radius: 8px !important;
    }
  `]
})
export class FarmMapComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapContainer') mapContainer!: ElementRef;
  private map?: L.Map;

  ngAfterViewInit(): void {
    this.initMap();
  }

  private initMap(): void {
    // 1. Camadas
    const googleRoads = L.tileLayer('https://{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}', {
      maxZoom: 20, subdomains: ['mt0', 'mt1', 'mt2', 'mt3'], attribution: '© Google'
    });

    const googleSatellite = L.tileLayer('https://{s}.google.com/vt/lyrs=s,h&x={x}&y={y}&z={z}', {
      maxZoom: 20, subdomains: ['mt0', 'mt1', 'mt2', 'mt3'], attribution: '© Google'
    });

    // 2. Coordenadas do LocalStorage
    const savedLocation = localStorage.getItem('user_location');
    let coords: L.LatLngExpression = [-23.56183, -46.63777];

    if (savedLocation) {
      try {
        const parsed = JSON.parse(savedLocation);
        coords = [parsed.lat, parsed.lng];
      } catch (e) { console.error(e); }
    }

    // 3. Inicialização do Mapa
    this.map = L.map(this.mapContainer.nativeElement, {
      center: coords,
      zoom: 16, // Aumentei o zoom para ver o ponto melhor
      layers: [googleSatellite],
      zoomControl: false
    });

    L.control.zoom({ position: 'bottomright' }).addTo(this.map);

    L.circleMarker(coords, {
      radius: 8,
      fillColor: "#00b7ff",
      color: "#000",
      weight: 1,
      opacity: 1,
      fillOpacity: 0.8
    }).addTo(this.map).bindTooltip("Minha Localização: " + coords);

    // 5. Controle de Camadas
    const baseMaps = { "Mapa": googleRoads, "Satélite": googleSatellite };
    L.control.layers(baseMaps, undefined, { position: 'bottomleft' }).addTo(this.map);

    // 6. Correção de frames
    setTimeout(() => {
      this.map?.invalidateSize();
    }, 300);
  }

  ngOnDestroy(): void {
    if (this.map) { this.map.remove(); }
  }
}