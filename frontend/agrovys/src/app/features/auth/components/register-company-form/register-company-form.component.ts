import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { NgxMaskDirective, NgxMaskPipe, provideNgxMask } from 'ngx-mask';
import { catchError, debounceTime, distinctUntilChanged, filter, map, of, switchMap } from 'rxjs';
import { AddressVerifyService } from '../../../../shared/services/address-verify.service';

@Component({
  selector: 'app-register-company-form',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule,
    NgxMaskDirective,
    NgxMaskPipe,
  ],
  templateUrl: './register-company-form.component.html',
  styleUrl: './register-company-form.component.scss',
  providers: [provideNgxMask()],
})
export class RegisterCompanyFormComponent implements OnInit {
  formRegisterCompany: FormGroup;
  submitted = false;
  
  // Variáveis para a lógica de endereço
  isLoadingData = false;
  states: any[] = [];
  cities: any[] = [];

  constructor(
    private fb: FormBuilder,
    private addressService: AddressVerifyService
  ) {
    this.formRegisterCompany = this.fb.group({
      businessType: ['Fazenda', Validators.required],
      organizationName: ['', Validators.required],
      description: [''],
      cep: ['', [Validators.required, Validators.minLength(8)]],
      street: ['', Validators.required],
      number: ['', Validators.required],
      neighborhood: ['', Validators.required], // Adicionado Bairro
      complement: [''],
      idState: ['', Validators.required],
      idCity: [{ value: '', disabled: true }, Validators.required],
    });
  }

  ngOnInit(): void {
    this.getStates();
    this.watchStateChanges();
    this.watchZipCodeChanges();
  }

  get f() {
    return this.formRegisterCompany.controls;
  }

  // --- LÓGICA DE ENDEREÇO (Igual ao do Usuário) ---
  getStates() {
    this.addressService.getState().subscribe((res) => (this.states = res));
  }

  watchZipCodeChanges() {
    this.formRegisterCompany.get('cep')?.valueChanges.pipe(
      map(v => v ? v.replace(/\D/g, '') : ''),
      filter(v => v.length === 8),
      distinctUntilChanged(),
      switchMap(cep => {
        this.isLoadingData = true;
        return this.addressService.getZipCode(cep).pipe(
          catchError(() => of({ erro: true }))
        );
      })
    ).subscribe((data: any) => {
      this.isLoadingData = false;

      if (data && !data.erro) {
        this.formRegisterCompany.patchValue({
          street: data.logradouro,
          neighborhood: data.bairro,
          idState: data.uf, 
          complement: data.complemento || ''
        }, { emitEvent: true }); 
        
        this.loadCities(data.uf, data.localidade);
      } else {
        this.formRegisterCompany.get('idCity')?.disable();
        this.formRegisterCompany.get('idCity')?.setValue('');
        this.cities = [];
      }
    });
  }

  watchStateChanges() {
    this.formRegisterCompany
      .get('idState')
      ?.valueChanges.pipe(distinctUntilChanged())
      .subscribe((sigla) => {
        if (sigla) {
          this.formRegisterCompany.get('idCity')?.enable();
          this.loadCities(sigla);
        } else {
          this.formRegisterCompany.get('idCity')?.disable();
          this.cities = [];
        }
      });
  }

  loadCities(sigla: string, cityToSelect?: string) {
    this.isLoadingData = true;
    this.addressService.getCities(sigla).subscribe({
      next: (res) => {
        this.cities = res;
        this.isLoadingData = false;
        if (cityToSelect) {
          const found = this.cities.find(
            (c) => c.nome.toLowerCase() === cityToSelect.toLowerCase(),
          );
          if (found)
            this.formRegisterCompany.get('idCity')?.setValue(found.id || found.nome);
        }
      },
      error: () => (this.isLoadingData = false),
    });
  }

  // --- SAÍDA DOS DADOS PARA O COMPONENTE PAI ---
  public getFormData() {
    this.submitted = true;
    const raw = this.formRegisterCompany.getRawValue();
    
    return {
      isValid: this.formRegisterCompany.valid,
      // Mapeamos os dados internos para o formato que a API/Componente Pai espera
      data: {
        businessType: raw.businessType,
        organizationName: raw.organizationName,
        description: raw.description,
        cep: raw.cep.replace(/\D/g, ''), // Limpa o CEP antes de enviar
        street: raw.street,
        number: raw.number,
        neighborhood: raw.neighborhood,
        complement: raw.complement,
        state: raw.idState, // Aqui vai enviar "SP", resolvendo o erro do banco!
        city: raw.idCity,
      }
    };
  }
}