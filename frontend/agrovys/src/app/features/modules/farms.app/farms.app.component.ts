import { CommonModule } from '@angular/common';
import { Component, ViewChild } from '@angular/core';
import { FarmMapComponent } from './components/farm-map/farm-map.component';
import {
  ModalAction,
  ModalComponent,
} from '../../../shared/components/modal/modal.component';
import { FarmNewComponent } from './components/farm-new/farm-new.component';
import { LocalStorageUtils } from '../../../core/utils/localstorage';
import { FarmService } from './services/farm.service';
import { ListFarmsModel } from './models/farm.model';
import { finalize, firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-farms.app',
  standalone: true,
  imports: [CommonModule, FarmMapComponent, ModalComponent, FarmNewComponent],
  templateUrl: './farms.app.component.html',
  styleUrl: './farms.app.component.scss',
})
export class FarmsAppComponent {
  @ViewChild('newFarm') newFarmComponent!: FarmNewComponent;

  title = 'Fazendas';
  description = 'Gerenciamento de Limites, Talhões e Linhas de Orientação';

  isLoadingData = true;
  isLoadingNewFarm = false;
  isVisibleNewFarm = false;

  farms: ListFarmsModel[] = [];
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

  constructor(private farmService: FarmService) {}

  ngOnInit(): void {
    this.loadData();
  }

async loadData() {
    this.isLoadingData = true;

    try {
      await Promise.all([
        this.loadFarms(),
      ]);
      
      console.log('Todos os dados foram carregados com sucesso!');
    } catch (error) {
      console.error('Erro ao carregar dados da página:', error);
    } finally {
      this.isLoadingData = false;
    }
  }

  async loadFarms(): Promise<void> {
    this.farms = await firstValueFrom(this.farmService.getFarmsList());
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
}
