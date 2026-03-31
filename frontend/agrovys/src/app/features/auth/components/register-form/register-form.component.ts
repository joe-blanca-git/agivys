import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  AbstractControl,
  AsyncValidatorFn,
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { NgxMaskDirective, NgxMaskPipe, provideNgxMask } from 'ngx-mask';
import {
  map,
  catchError,
  debounceTime,
  switchMap,
  of,
  distinctUntilChanged,
  filter,
} from 'rxjs';
import { AuthService } from '../../../../core/auth/auth.service';
import { AddressVerifyService } from '../../../../shared/services/address-verify.service';

@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NgxMaskDirective,
    NgxMaskPipe,
  ],
  templateUrl: './register-form.component.html',
  styleUrl: './register-form.component.scss',
  providers: [provideNgxMask()],
})
export class RegisterFormComponent implements OnInit {
  formRegister: FormGroup;
  submitted = false;
  isLoadingData = false;
  states: any[] = [];
  cities: any[] = [];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private addressService: AddressVerifyService,
  ) {
    this.formRegister = this.fb.group(
      {
        name: ['', [Validators.required]],
        email: [
          '',
          [Validators.required, Validators.email],
          [this.validateExistingEmail(this.authService)],
        ],
        document: [
          '',
          [Validators.required],
          [this.validateExistingDocument(this.authService)],
        ],
        phone: ['', [Validators.required]],
        password: [
          '',
          [
            Validators.required,
            Validators.minLength(8),
            this.passwordStrengthValidator,
          ],
        ],
        passwordConfirmation: ['', [Validators.required]],
        dtBirth: ['', [Validators.required]],
        cep: ['', [Validators.required, Validators.minLength(8)]],
        street: ['', [Validators.required]],
        number: ['', [Validators.required]],
        neighborhood: ['', [Validators.required]],
        complement: [''],
        idState: ['', [Validators.required]],
        idCity: [{ value: '', disabled: true }, [Validators.required]],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  ngOnInit(): void {
    this.getStates();
    this.watchStateChanges();
    this.watchZipCodeChanges();
  }

  get f() {
    return this.formRegister.controls;
  }

  getStates() {
    this.addressService.getState().subscribe((res) => (this.states = res));
  }

  watchZipCodeChanges() {
    this.formRegister.get('cep')?.valueChanges.pipe(
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
        // Força a atualização dos campos com os dados corretos da API (sigla do estado)
        // Isso sobrescreve a bagunça que o preenchimento automático do navegador faz
        this.formRegister.patchValue({
          street: data.logradouro,
          neighborhood: data.bairro,
          idState: data.uf, 
          complement: data.complemento || ''
        }, { emitEvent: true }); // Garante que o Angular perceba a mudança
        
        this.loadCities(data.uf, data.localidade);
      } else {
        // Se o CEP for inválido, apenas limpa a cidade, mas mantém os estados intactos
        this.formRegister.get('idCity')?.disable();
        this.formRegister.get('idCity')?.setValue('');
        this.cities = [];
      }
    });
  }

  patchAddressData(data: any) {
    this.formRegister.patchValue({
      street: data.logradouro,
      neighborhood: data.bairro,
      idState: data.uf,
    });
    this.loadCities(data.uf, data.localidade);
  }

  watchStateChanges() {
    this.formRegister
      .get('idState')
      ?.valueChanges.pipe(distinctUntilChanged())
      .subscribe((sigla) => {
        if (sigla) {
          this.formRegister.get('idCity')?.enable();
          this.loadCities(sigla);
        } else {
          this.formRegister.get('idCity')?.disable();
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
            this.formRegister.get('idCity')?.setValue(found.id || found.nome);
        }
      },
      error: () => (this.isLoadingData = false),
    });
  }

  public getFormData() {
    const raw = this.formRegister.getRawValue();
    return {
      isValid: this.formRegister.valid,
      userBody: {
        name: raw.name,
        document: raw.document.replace(/\D/g, ''),
        email: raw.email,
        password: raw.password,
        birthDate: this.formatISO(raw.dtBirth),
        AddressDescription: 'Principal',
        zipCode: raw.cep.replace(/\D/g, ''),
        street: raw.street,
        number: raw.number,
        complement: raw.complement,
        neighborhood: raw.neighborhood,
        city: raw.idCity,
        state: raw.idState,
      }
    };
  }

private formatISO(dateStr: string): string {
    if (!dateStr) return '';
    
    // Limpa qualquer coisa que não seja número (tira as barras caso existam)
    const cleanDate = dateStr.replace(/\D/g, '');
    
    // Se não tiver exatamente 8 números (DDMMYYYY), retorna vazio
    if (cleanDate.length !== 8) return ''; 

    const d = cleanDate.substring(0, 2);
    const m = cleanDate.substring(2, 4);
    const y = cleanDate.substring(4, 8);
    
    // Retorna exatamente YYYY-MM-DD para o C#
    return `${y}-${m}-${d}`;
  }

  validateExistingDocument(auth: AuthService): AsyncValidatorFn {
    return (c: AbstractControl) => {
      const doc = c.value?.replace(/\D/g, '');
      if (!doc || doc.length !== 11) return of(null);
      return auth.verifyExitingDocument(doc).pipe(
        debounceTime(400),
        map((res) => (res.exists ? { documentExisting: true } : null)),
        catchError(() => of(null)),
      );
    };
  }

  validateExistingEmail(auth: AuthService): AsyncValidatorFn {
    return (c: AbstractControl) => {
      if (!c.value) return of(null);
      return auth.verifyExitingEmail(c.value).pipe(
        debounceTime(400),
        map((res) => (res.exists ? { emailExisting: true } : null)),
        catchError(() => of(null)),
      );
    };
  }

  passwordStrengthValidator(c: AbstractControl): ValidationErrors | null {
    const v = c.value || '';
    return !/[A-Z]/.test(v) || !/[a-z]/.test(v) || !/[0-9]/.test(v)
      ? { passwordStrength: true }
      : null;
  }

  passwordMatchValidator(g: AbstractControl): ValidationErrors | null {
    const p = g.get('password')?.value;
    const cp = g.get('passwordConfirmation')?.value;
    if (p !== cp)
      g.get('passwordConfirmation')?.setErrors({ passwordMismatch: true });
    return p !== cp ? { passwordMismatch: true } : null;
  }
}
