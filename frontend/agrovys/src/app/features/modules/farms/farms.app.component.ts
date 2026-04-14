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

@Component({
  selector: 'app-farms.app',
  standalone: true,
  imports: [CommonModule, FarmMapComponent, ModalComponent, FarmNewComponent, FarmNewSimpleComponent, FormsModule],
  templateUrl: './farms.app.component.html',
  styleUrl: './farms.app.component.scss',
})
export class FarmsAppComponent {
  @ViewChild('newFarm') newFarmComponent!: FarmNewComponent;
  @ViewChild('newFarmSimple') newFarmSimpleComponent!: FarmNewSimpleComponent;

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
  actionsModal: ModalAction[] = [
    {
      class: 'btn-outline-secondary',
      type: 'closeNewFarm',
      icon: 'fa fa-xmark',
      text: 'Fechar',
      title: 'teste',
    },
    {
      class: 'btn-agrovys-primary',
      type: 'newFarm',
      icon: 'fa fa-plus',
      text: 'Cadastrar Fazenda',
      title: 'teste',
    },
  ];

  actionsModalSimple: ModalAction[] = [
    {
      class: 'btn-outline-secondary',
      type: 'closeNewFarmSimple',
      icon: 'fa fa-xmark',
      text: 'Fechar',
      title: 'teste',
    },
    {
      class: 'btn-agrovys-primary',
      type: 'newFarmSimple',
      icon: 'fa fa-plus',
      text: 'Cadastrar',
      title: 'teste',
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
    }
  }

  handleLoadingNewFarm(event: boolean) {
    this.isLoadingNewFarm = event;
  }

  async handleSuccessNewFarm() {
    this.isVisibleNewFarm = false;
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

    const selectedFarmIds = this.farms.filter(f => f.selected).map(f => f.id);
    console.log('Arquivando fazendas (IDs):', selectedFarmIds);

    this.farmService.archiveFarms(selectedFarmIds).subscribe({
      next: (res) => {
        console.log('Fazendas arquivadas com sucesso');
        this.loadData();
      },
      error: (err) => console.error('Erro ao arquivar:', err)
    });
  }


}
