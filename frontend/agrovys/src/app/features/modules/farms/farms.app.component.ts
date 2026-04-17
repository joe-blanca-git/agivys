import { CommonModule } from '@angular/common';
import { Component, ViewChild } from '@angular/core';
import { FarmMapComponent } from './components/farm-map/farm-map.component';
import {
  ModalAction,
  ModalComponent,
} from '../../../shared/components/modal/modal.component';
import { FarmNewComponent } from './components/farm-new/farm-new.component';
import { FarmNewSimpleComponent } from './components/farm-new-simple/farm-new-simple.component';
import { FarmService } from './services/farm.service';
import { ListFarmsModel } from './models/farm.model';
import { firstValueFrom } from 'rxjs';
import { ClientService } from './services/client.service';
import { FormsModule } from '@angular/forms';
import { FarmExportComponent } from "./components/farm-export/farm-export.component";

@Component({
  selector: 'app-farms.app',
  standalone: true,
  imports: [CommonModule, FarmMapComponent, ModalComponent, FarmNewComponent, FarmNewSimpleComponent, FormsModule, FarmExportComponent],
  templateUrl: './farms.app.component.html',
  styleUrl: './farms.app.component.scss',
})
export class FarmsAppComponent {
  @ViewChild('newFarm') newFarmComponent!: FarmNewComponent;
  @ViewChild('newFarmSimple') newFarmSimpleComponent!: FarmNewSimpleComponent;
  @ViewChild('farmExport') farmExportComponent!: FarmExportComponent;

  title = 'Fazendas';
  description = 'Limites, Talhões e Linhas de Orientação';

  isLoadingData = false;
  isLoadingListFarms = false;
  isLoadingNewFarm = false;
  isVisibleNewFarm = false;

  isLoadingNewFarmSimple = false;
  isVisibleNewFarmSimple = false;

  farms: ListFarmsModel[] = [];
  clients: any[] = [];
  limitesGeoJson: any = null;

  isVisibleExport = false;
  isExportTypeSelected = false;

  actionsModal: ModalAction[] = [
    {
      class: 'btn-agrovys-primary',
      type: 'newFarm',
      icon: 'fa fa-plus',
      text: 'Cadastrar Fazenda',
      title: 'Cadastrar Fazenda',
    },
  ];

  actionsModalSimple: ModalAction[] = [

    {
      class: 'btn-agrovys-primary',
      type: 'newFarmSimple',
      icon: 'fa fa-plus',
      text: 'Cadastrar',
      title: 'Cadastrar',
    },
  ];

  actionsModalExport: ModalAction[] = [

    {
      class: 'btn-agrovys-primary',
      type: 'export',
      icon: 'fa fa-download',
      text: 'Exportar',
      title: 'Exportar',
    },
  ];

  constructor(private farmService: FarmService, private clientService: ClientService) { }

  ngOnInit(): void {
    this.loadData();
  }

  async loadData() {
    this.isLoadingData = true;

    try {
      await Promise.all(
        [
          this.loadFarms(),
          this.loadBondaryGeneral()
          //  this.loadClients()
        ]);

      console.log('Todos os dados foram carregados com sucesso!');
    } catch (error) {
      console.error('Erro ao carregar dados da página:', error);
    } finally {
      this.isLoadingData = false;
    }
  }

  async loadClients(): Promise<void> {
    this.clients = await firstValueFrom(this.clientService.getClients());
  }

  async loadBondaryGeneral(): Promise<void> {
    try {
      const geojson = await firstValueFrom(this.farmService.getAllBoundariesGeoJSON());
      this.limitesGeoJson = geojson;
    } catch (e) {
      console.error('Erro ao buscar GeoJSON geral:', e);
    }
  }

  async loadFarms(): Promise<void> {
    const farms = await firstValueFrom(this.farmService.getFarmsList());
    this.farms = farms.sort((a, b) => a.name.localeCompare(b.name));
  }

  onSaveNewFarm() {
    this.newFarmComponent.submitNewFarm();
  }

  handleNewFarm() {
    this.isVisibleNewFarm = !this.isVisibleNewFarm;

    if (this.isVisibleNewFarm) {
      console.log('abriu');
    }
  }

  handleModalAction(actionType: string) {
    if (actionType === 'closeNewFarm') {
      this.isVisibleNewFarm = false;
    } else if (actionType === 'newFarm') {
      this.onSaveNewFarm();
    } else if (actionType === 'closeExport') {
      this.isVisibleExport = false;
    } else if (actionType === 'export') {
      this.farmExportComponent.exportFarms();
    }
  }

  handleLoadingNewFarm(event: boolean) {
    this.isLoadingNewFarm = event;
  }

  async handleSuccessNewFarm() {
    this.isVisibleNewFarm = false;
    this.isLoadingListFarms = true;
    console.log(this.isLoadingListFarms);
    try {
      await Promise.all([
        this.loadFarms(),
        this.loadBondaryGeneral()
      ]);
    } catch (error) {
      console.error(error);
    } finally {
      this.isLoadingListFarms = false;
      console.log(this.isLoadingListFarms);
    }
  }

  get hasSelectedFarms(): boolean {
    return this.farms.some(f => f.selected);
  }

  onSaveNewFarmSimple() {
    this.newFarmSimpleComponent.submitNewFarm();
  }

  handleNewFarmSimple() {
    this.isVisibleNewFarmSimple = !this.isVisibleNewFarmSimple;
  }

  handleModalActionSimple(actionType: string) {
    if (actionType === 'closeNewFarmSimple') {
      this.isVisibleNewFarmSimple = false;
    } else if (actionType === 'newFarmSimple') {
      this.onSaveNewFarmSimple();
    }
  }

  handleLoadingNewFarmSimple(event: boolean) {
    this.isLoadingNewFarmSimple = event;
  }

  async handleSuccessNewFarmSimple() {
    this.isVisibleNewFarmSimple = false;
    this.isLoadingListFarms = true;
    try {
      await Promise.all([
        this.loadFarms(),
        this.loadBondaryGeneral()
      ]);
    } catch (error) {
      console.error(error);
    } finally {
      this.isLoadingListFarms = false;
    }
  }

  archiveSelectedFarms() {
    if (!this.hasSelectedFarms) return;

    this.isLoadingListFarms = true;
    const selectedFarmIds = this.farms.filter(f => f.selected).map(f => f.id);

    this.farmService.archiveFarms(selectedFarmIds).subscribe({
      next: async (res) => {
        try {
          await Promise.all([
            this.loadFarms(),
            this.loadBondaryGeneral()
          ]);
        } catch (error) {
          console.error('Erro ao recarregar dados após arquivamento:', error);
        } finally {
          this.isLoadingListFarms = false;
        }
      },
      error: (err) => {
        console.error('Erro ao arquivar:', err);
        this.isLoadingListFarms = false;
      }
    });
  }

  async onToggleFarm(farm: ListFarmsModel) {
    if (farm.boundaries && farm.boundaries.length > 0) return;

    farm.isLoadingBoundaries = true;
    try {
      const boundaries = await firstValueFrom(this.farmService.getFarmBoundaries(farm.id));
      farm.boundaries = boundaries;
    } catch (e) {
      console.error('Erro ao carregar talhões:', e);
    } finally {
      farm.isLoadingBoundaries = false;
    }
  }

  handleExport() {
    if (!this.hasSelectedFarms) return;
    this.isExportTypeSelected = false;
    this.isVisibleExport = true;
  }

  handleStatusExport(event: boolean) {
    this.isExportTypeSelected = event;
  }


  getSelectedFarms(): ListFarmsModel[] {
    return this.farms.filter(f => f.selected);
  }

}
