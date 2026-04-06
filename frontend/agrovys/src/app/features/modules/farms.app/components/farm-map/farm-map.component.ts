import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  Input,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import * as L from 'leaflet';

@Component({
  selector: 'app-farm-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './farm-map.component.html',
  styleUrl: './farm-map.component.scss',
})
export class FarmMapComponent {
  @Input() shapes: any;
  @Input() shapesJd: any;

  @ViewChild('mapContainer') mapContainer!: ElementRef;

  private map!: L.Map;
  private polygonLayer = L.layerGroup();
  private shapesJdLayer = L.layerGroup();

  ngOnInit(): void {}

  ngOnChanges(changes: SimpleChanges): void {
    if (this.map) {
      if (changes['shapes']) {
        this.mostrarShapes();
      }
      if (changes['shapesJd']) {
        this.mostrarShapesJd();
      }
    }
  }

  ngAfterViewInit(): void {
    this.initMap();
  }

  ngOnDestroy(): void {
    // Garante a destruição ao sair
    if (this.map) {
      this.map.remove();
      // Opcional: forçar limpeza da variável
      (this.map as any) = null;
    }
  }

  private initMap(): void {
    // PROTEÇÃO EXTRA:
    // Se por acaso a variável this.map foi perdida mas o elemento HTML
    // ainda tiver a sujeira do Leaflet, limpamos manualmente.
    if (this.map) {
      this.map.remove();
    }

    // Verificação de segurança para o elemento nativo
    const container = this.mapContainer.nativeElement;
    if (!container) return;

    // Se o container já tiver um ID interno do Leaflet (o erro que você estava vendo),
    // isso significa que o mapa já foi iniciado nele.
    // Vamos tentar limpar a instância anterior se ela existir no DOM.
    if ((container as any)._leaflet_id) {
      // Tenta encontrar a instância antiga e remover, ou apenas ignora se já estiver ok
      // Mas geralmente, usar ViewChild resolve a duplicação de IDs.
    }

    const savedLocation = localStorage.getItem('user_location');
    let centerCoords: L.LatLngExpression = [-23.56183, -46.63777];

    const openStreetMap = L.tileLayer(
      'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
      {
        attribution: '© OpenStreetMap contributors',
        maxZoom: 19,
      },
    );

    const esriImagery = L.tileLayer(
      'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
      {
        attribution: 'Tiles © Esri — Source: Esri, USGS, etc.',
        maxZoom: 19,
      },
    );

    // MUDANÇA PRINCIPAL AQUI:
    // Passamos o elemento HTML direto (container), em vez da string 'map'
    this.map = L.map(container, {
      center: centerCoords,
      zoom: 8,
      layers: [esriImagery],
    });

    const baseMaps = {
      Mapa: openStreetMap,
      Satélite: esriImagery,
    };

    L.control
      .layers(baseMaps, undefined, { position: 'bottomright' })
      .addTo(this.map);

    this.polygonLayer.addTo(this.map);
    this.shapesJdLayer.addTo(this.map);

    this.mostrarShapes();
    this.mostrarShapesJd();
  }

  public mostrarShapes(): void {
    // Verifica se layers e map existem antes de limpar
    if (!this.polygonLayer || !this.map) return;

    this.polygonLayer.clearLayers();
    const bounds = L.latLngBounds([]);

    if (this.shapes && this.shapes.length > 0) {
      this.shapes.forEach((polygon: any) => {
        if (polygon.shape) {
          const latlngRings = this.parseWkt(polygon.shape);
          this.createPolygon(
            latlngRings,
            '#0cae8bff',
            `Talhão: ${polygon.codTalhao}`,
            this.polygonLayer,
            bounds,
          );
        }
      });

      if (bounds.isValid()) {
        this.map.fitBounds(bounds, { padding: [20, 20] });
      }
    }
  }

  public mostrarShapesJd(): void {
    if (!this.shapesJdLayer || !this.map) return;

    this.shapesJdLayer.clearLayers();
    const bounds = L.latLngBounds([]); // Opcional se quiser focar no JD

    if (this.shapesJd && this.shapesJd.length > 0) {
      this.shapesJd.forEach((polygon: any) => {
        if (polygon.shape) {
          const latlngRings = this.parseWkt(polygon.shape);
          this.createPolygon(
            latlngRings,
            'yellow',
            'JD',
            this.shapesJdLayer,
            undefined,
          );
        }
      });
    }
  }

  public ocultarShapes(): void {
    if (this.polygonLayer) {
      this.polygonLayer.clearLayers();
    }
  }

  public ocultarShapesJd(): void {
    if (this.shapesJdLayer) {
      this.shapesJdLayer.clearLayers();
    }
  }

  private parseWkt(wkt: string): L.LatLng[][] {
    const result: L.LatLng[][] = [];
    if (!wkt) return result;

    if (wkt.startsWith('MULTIPOLYGON')) {
      wkt = wkt.replace(/^MULTIPOLYGON\s*\(\(\(/, '').replace(/\)\)\)\s*$/, '');
      const polygonStrings = wkt.split(/\)\s*\),\s*\(\(/);
      polygonStrings.forEach((polyStr) => {
        const rings = polyStr.split(/\)\s*,\s*\(/);
        result.push(...rings.map((ring) => this.convertWktToLatLngs(ring)));
      });
    } else if (wkt.startsWith('POLYGON')) {
      wkt = wkt.replace(/^POLYGON\s*\(\(/, '').replace(/\)\)\s*$/, '');
      const rings = wkt.split(/\)\s*,\s*\(/);
      result.push(...rings.map((ring) => this.convertWktToLatLngs(ring)));
    }
    return result;
  }

  private convertWktToLatLngs(ring: string): L.LatLng[] {
    return ring.split(',').map((point) => {
      const [lngStr, latStr] = point.trim().split(/\s+/);
      return L.latLng(parseFloat(latStr), parseFloat(lngStr));
    });
  }

  private createPolygon(
    latlngRings: L.LatLng[][],
    color: string,
    tooltip: string,
    layerGroup: L.LayerGroup,
    bounds?: L.LatLngBounds,
  ): void {
    const polygon = L.polygon(latlngRings, {
      color: color,
      fillOpacity: 0.2,
      weight: 2,
    });

    polygon.bindTooltip(tooltip, {
      sticky: true,
      direction: 'top',
    });

    polygon.addTo(layerGroup);
    if (bounds) {
      latlngRings.flat().forEach((latlng) => bounds.extend(latlng));
    }
  }
}
