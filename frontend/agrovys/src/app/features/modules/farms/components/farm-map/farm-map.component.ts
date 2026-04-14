import { Component, ElementRef, ViewChild, AfterViewInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import maplibregl from 'maplibre-gl';

@Component({
  selector: 'app-farm-map',
  standalone: true,
  imports: [CommonModule],
  template: `<div #mapContainer class="map-container"></div>`,
  styles: [`
    :host { display: block; width: 100%; height: 100%; }
    .map-container { width: 100%; height: 100%; }
  `]
})
export class FarmMapComponent implements AfterViewInit, OnDestroy, OnChanges {
  @ViewChild('mapContainer') mapContainer!: ElementRef;
  @Input() limitesGeoJson: any = null;

  private map!: maplibregl.Map;

  ngAfterViewInit(): void {
    this.initMap();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['limitesGeoJson'] && this.map && this.map.isStyleLoaded()) {
      this.atualizarLimites();
    }
  }

  private initMap(): void {

    // Inicializa o mapa com a imagem de Satélite do Google
    const savedLocation = localStorage.getItem('user_location');
    let coords: maplibregl.LngLatLike = [-46.63777, -23.56183]; // [Longitude, Latitude]
    if (savedLocation) {
      try {
        const parsed = JSON.parse(savedLocation);
        coords = [parsed.lng, parsed.lat];
      } catch (e) { console.error(e); }
    }

    this.map = new maplibregl.Map({
      container: this.mapContainer.nativeElement,
      center: coords,
      zoom: 14,
      style: {
        version: 8,
        sources: {
          'google-satelite': {
            type: 'raster',
            tiles: ['https://mt1.google.com/vt/lyrs=s&x={x}&y={y}&z={z}'],
            tileSize: 256
          }
        },
        layers: [
          {
            id: 'fundo-satelite',
            type: 'raster',
            source: 'google-satelite',
            minzoom: 0,
            maxzoom: 22
          }
        ]
      }
    });

    // Adiciona controles de navegação (Zoom e Rotação)
    this.map.addControl(new maplibregl.NavigationControl(), 'bottom-right');

    // Quando o mapa terminar de carregar o fundo, criamos a camada de pintura dos talhões
    this.map.on('load', () => {
      this.map.addSource('fonte-limites', {
        type: 'geojson',
        data: this.limitesGeoJson || { type: 'FeatureCollection', features: [] }
      });

      this.map.addLayer({
        id: 'camada-limites',
        type: 'fill',
        source: 'fonte-limites',
        paint: {
          'fill-color': '#00b7ff', // Cor interna
          'fill-opacity': 0.4,     // Transparência interna
          'fill-outline-color': '#ffffff' // Cor da borda
        }
      });

      this.atualizarLimites();
    });
  }

  private atualizarLimites(): void {
    if (!this.limitesGeoJson) return;

    const source = this.map.getSource('fonte-limites') as maplibregl.GeoJSONSource;
    if (source) {
      source.setData(this.limitesGeoJson);
      this.fitMapToBounds();
    }
  }

  private fitMapToBounds(): void {
    if (!this.limitesGeoJson?.features?.length) return;

    const bounds = new maplibregl.LngLatBounds();

    this.limitesGeoJson.features.forEach((feature: any) => {
      if (feature.geometry?.coordinates) {
        this.extendBoundsWithCoords(bounds, feature.geometry.coordinates);
      }
    });

    if (!bounds.isEmpty()) {
      this.map.fitBounds(bounds, { padding: 40, maxZoom: 16 });
    }
  }

  private extendBoundsWithCoords(bounds: maplibregl.LngLatBounds, coords: any[]): void {
    if (coords.length === 2 && typeof coords[0] === 'number' && typeof coords[1] === 'number') {
      bounds.extend(coords as [number, number]);
      return;
    }
    for (const c of coords) {
      if (Array.isArray(c)) {
        this.extendBoundsWithCoords(bounds, c);
      }
    }
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
  }
}