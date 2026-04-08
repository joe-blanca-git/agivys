import { CommonModule } from '@angular/common';
import { Component, ViewChild } from '@angular/core';
import { FarmMapComponent } from './components/farm-map/farm-map.component';
import {
  ModalAction,
  ModalComponent,
} from '../../../shared/components/modal/modal.component';
import { FarmNewComponent } from './components/farm-new/farm-new.component';
import { LocalStorageUtils } from '../../../core/utils/localstorage';

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

  farms: any[] = [];
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

  ngOnInit(): void {    
    setTimeout(() => {
      this.isLoadingData = false;
    }, 2000);

    this.farms = [
      {
        farmId: 1,
        farmName: 'Fazenda Mata Nova',
        description: 'Produção de soja e milho, região norte.',
        area: 1200,
        fields: [
          { fieldId: 101, name: 'Talhão A1', area: 500 },
          { fieldId: 102, name: 'Talhão A2', area: 700 },
        ],
      },
      {
        farmId: 2,
        farmName: 'Estância do Sol',
        description: 'Área de pastagem e pecuária intensiva.',
        area: 850,
        fields: [
          { fieldId: 201, name: 'Piquete Norte', area: 400 },
          { fieldId: 202, name: 'Piquete Sul', area: 450 },
        ],
      },
      {
        farmId: 3,
        farmName: 'Sítio Recanto Feliz',
        description: 'Hortifruti orgânico e estufas.',
        area: 45,
        fields: [
          { fieldId: 301, name: 'Canteiro Central', area: 20 },
          { fieldId: 302, name: 'Estufa 01', area: 25 },
        ],
      },
      {
        farmId: 4,
        farmName: 'Fazenda Rio Claro',
        description: 'Reserva florestal e plantio de eucalipto.',
        area: 3000,
        fields: [
          { fieldId: 401, name: 'Setor Florestal', area: 1500 },
          { fieldId: 402, name: 'Área de Manejo', area: 1500 },
        ],
      },
      {
        farmId: 5,
        farmName: 'Agro Vale',
        description: 'Cultivo de cana-de-açúcar.',
        area: 500,
        fields: [{ fieldId: 501, name: 'Gleba 01', area: 500 }],
      },
    ];
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
