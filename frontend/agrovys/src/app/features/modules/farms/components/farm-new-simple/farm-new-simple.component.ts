import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { FarmService } from '../../services/farm.service';
import { ClientService } from '../../services/client.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { LocalStorageUtils } from '../../../../../core/utils/localstorage';
import { loggUser } from '../../../../../shared/models/loggUser';

@Component({
  selector: 'app-farm-new-simple',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './farm-new-simple.component.html',
  styleUrl: './farm-new-simple.component.scss',
})
export class FarmNewSimpleComponent implements OnInit {
  @Output() loadingEmit = new EventEmitter<boolean>();
  @Output() successEmit = new EventEmitter<void>();

  localStorage = new LocalStorageUtils();

  isLoading: boolean = true;
  selectedYear!: number;
  listYears: any[] = [];
  errorMessage: string = '';

  clients: any[] = [];
  selectedClientUuid: string | null = null;
  farmName: string = '';

  constructor(
    private farmService: FarmService,
    private clientService: ClientService,
    private toastService: ToastService,
  ) { }

  ngOnInit(): void {
    const currentYear = new Date().getFullYear();
    this.selectedYear = currentYear;
    this.listYears = [];
    for (let i = -5; i <= 5; i++) {
        this.listYears.push(currentYear + i);
    }

    this.loadData();
  }

  async loadData() {
    this.isLoading = true;
    try {
      this.clients = await firstValueFrom(this.clientService.getClients());
      if (this.clients && this.clients.length > 0) {
        const firstValidClient = this.clients.find(c => c.agrovys_uuid != null);
        if (firstValidClient) {
          this.selectedClientUuid = firstValidClient.agrovys_uuid;
        }
      }
    } catch (error) {
      console.error('Erro ao carregar dados da página:', error);
    } finally {
      this.isLoading = false;
      this.loadingEmit.emit(false);
    }
  }

  public submitNewFarm() {
    if (!this.farmName || !this.selectedClientUuid || !this.selectedYear) {
      this.errorMessage = 'Verifique os campos obrigatórios para cadastrar a fazenda.';
      return;
    }
    this.errorMessage = '';

    this.isLoading = true;
    this.loadingEmit.emit(true);

    const user: loggUser = this.localStorage.getUser();

    const farmForm = {
      name: this.farmName,
      client_unit_id: this.selectedClientUuid,
      crop_year: String(this.selectedYear),
      agivys_user_id: String(user.id)
    };

    this.farmService
      .createSimpleFarm(farmForm)
      .subscribe({
        next: () => {
          this.toastService.success('Fazenda Criada com Sucesso!', 3000);
          this.isLoading = false;
          this.loadingEmit.emit(false);
          this.successEmit.emit();
        },
        error: (err) => {
          console.error('Erro ao cadastrar fazenda', err);
          this.toastService.error('Erro ao criar fazenda!');
          this.isLoading = false;
          this.loadingEmit.emit(false);
        },
      });
  }
}
