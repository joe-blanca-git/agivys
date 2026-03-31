import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  FormArray,
  ReactiveFormsModule,
} from '@angular/forms';
import { PersonService } from '../../services/person.service';
import { CommonModule } from '@angular/common';

import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-new-person-full',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NzModalModule,
    NzFormModule,
    NzInputModule,
    NzButtonModule,
    NzSelectModule,
    NzDatePickerModule,
  ],
  templateUrl: './new-person-full.component.html',
  styleUrl: './new-person-full.component.scss',
})
export class NewPersonFullComponent implements OnInit {
  @Input() isVisible: boolean = false;
  @Input() title: string = 'Cadastrar Pessoa <br> <span class="text-small text-secondary">(Cliente, Fornecedor ou Transportadora)</span>';
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<any>();

  public newPersonForm!: FormGroup;
  isLoadingData = false;

  constructor(
    private fb: FormBuilder,
    private personService: PersonService,
    private toastService: ToastService,
  ) {}

  ngOnInit(): void {
    this.initForm();
  }

  private initForm(): void {
    this.newPersonForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      userType: ['CLIENT', [Validators.required]],
      profileData: this.fb.group({
        name: ['', [Validators.required]],
        document: ['', [Validators.required]],
        birthDate: [null, [Validators.required]],
        phone: ['', [Validators.required]],
        phoneSecondary: [''],
        emailSecondary: ['', [Validators.email]],
        position: [''],
        addresses: this.fb.array([this.createAddressGroup()]),
      }),
    });
  }

  private createAddressGroup(): FormGroup {
    return this.fb.group({
      zipCode: ['', [Validators.required]],
      street: ['', [Validators.required]],
      number: ['', [Validators.required]],
      complement: [''],
      city: ['', [Validators.required]],
      state: ['', [Validators.required]],
      neighborhood: ['', [Validators.required]],
    });
  }

  get addresses(): FormArray {
    return this.newPersonForm.get('profileData.addresses') as FormArray;
  }

  addAddress(): void {
    this.addresses.push(this.createAddressGroup());
  }

  removeAddress(index: number): void {
    if (this.addresses.length > 1) {
      this.addresses.removeAt(index);
    }
  }

handleOk(): void {
  if (this.newPersonForm.valid) {
    this.isLoadingData = true;

    const rawData = this.newPersonForm.getRawValue(); 
    const cleanPayload = this.sanitizeData(rawData);

    this.personService.createPerson(cleanPayload).subscribe({
      next: (res) => {
        this.toastService.success('Pessoa Cadastrada com Sucesso.');
        this.saved.emit(res);
        this.handleCancel();
      },
      error: (err) => {
        console.error('Erro ao salvar:', err);
        this.isLoadingData = false;
      },
      complete: () => (this.isLoadingData = false),
    });
  } else {
    this.validateForm(this.newPersonForm);
  }
}

  private sanitizeData(data: any): any {
    if (typeof data !== 'object' || data === null) {
      return data === '' ? null : data;
    }

    Object.keys(data).forEach((key) => {
      data[key] = this.sanitizeData(data[key]);
    });

    return data;
  }

  private validateForm(formGroup: FormGroup | FormArray): void {
    Object.values(formGroup.controls).forEach((control) => {
      if (control instanceof FormGroup || control instanceof FormArray) {
        this.validateForm(control);
      } else {
        control.markAsDirty();
        control.updateValueAndValidity({ onlySelf: true });
      }
    });
  }

  handleCancel(): void {
    this.newPersonForm.reset();
    this.close.emit();
  }
}
