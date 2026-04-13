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
import { ClientService } from './services/client.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-farms.app',
  standalone: true,
  imports: [CommonModule, FarmMapComponent, ModalComponent, FarmNewComponent, FormsModule],
  templateUrl: './farms.app.component.html',
  styleUrl: './farms.app.component.scss',
})
export class FarmsAppComponent {
  @ViewChild('newFarm') newFarmComponent!: FarmNewComponent;

  title = 'Fazendas';
  description = 'Limites, Talhões e Linhas de Orientação';

  isLoadingData = true;
  isLoadingNewFarm = false;
  isVisibleNewFarm = false;

  farms: ListFarmsModel[] = [];
  clients: any[] = [];
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

  constructor(private farmService: FarmService, private clientService: ClientService) { }

  ngOnInit(): void {
    this.loadData();
  }

  async loadData() {
    this.isLoadingData = true;

    try {
      await Promise.all([this.loadFarms(), this.loadClients()]);
      console.log(this.clients);


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
    this.isLoadingData = true;
    try {
      await this.loadFarms();
    } catch (error) {
      console.error(error);
    } finally {
      this.isLoadingData = false;
    }
  }

  get hasSelectedFarms(): boolean {
    return this.farms.some(f => f.selected);
  }

  archiveSelectedFarms() {
    if (!this.hasSelectedFarms) return;
    
    const selectedFarms = this.farms.filter(f => f.selected);
    console.log('Arquivando fazendas:', selectedFarms);
    
    // TODO: Chamar o endpoint de arquivamento na API quando estiver pronto
    // this.farmService.archiveFarms(selectedFarms).subscribe({
    //   next: (res) => {
    //     console.log('Fazendas arquivadas com sucesso');
    //     this.loadData();
    //   },
    //   error: (err) => console.error('Erro ao arquivar:', err)
    // });
  }

  editFarmName(farm: any) {
    farm.isEditing = true;
    farm.tempName = farm.name;
  }

  cancelEditName(farm: any) {
    farm.isEditing = false;
  }

  saveFarmName(farm: any) {
    farm.isEditing = false;
    
    const oldName = farm.name;
    farm.name = farm.tempName;
    console.log(`Alterando nome da fazenda de ${oldName} para ${farm.name}`);
    
    // TODO: Chamar o endpoint para atualizar o nome da fazenda na API
    // this.farmService.updateFarmName(farm.agrovys_uuid, farm.name).subscribe({
    //   next: (res) => {
    //      console.log('Nome alterado com sucesso na API');
    //   },
    //   error: (err) => {
    //      console.error('Erro ao atualizar nome, revertendo...');
    //      farm.name = oldName;
    //   }
    // });
  }
}
