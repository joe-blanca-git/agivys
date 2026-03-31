import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { FinancialService } from '../../services/financial.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';

@Component({
  selector: 'app-new-financial-category',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NzButtonModule,
    NzModalModule,
    NzFormModule,
    NzSelectModule,
  ],
  templateUrl: './new-financial-category.component.html',
  styleUrl: './new-financial-category.component.scss',
})
export class NewFinancialCategoryComponent {
  @Input() isVisible: boolean = false;
  @Input() title: string = 'Nova Categoria Financeira';
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<any>();

  public newFinancialCategory!: FormGroup;
  isLoadingData = false;

  constructor(
    private fb: FormBuilder,
    private financialService: FinancialService,
    private toastService: ToastService,
  ) {}

  ngOnInit(): void {
    this.initForm();
  }

  private initForm(): void {
    this.newFinancialCategory = this.fb.group({
      name: ['', [Validators.required]],
      type: ['', [Validators.required]],
    });
  }

  handleOk(): void {
    if (this.newFinancialCategory.valid) {
      this.isLoadingData = true;

      const rawData = this.newFinancialCategory.getRawValue();
      const cleanPayload = this.sanitizeData(rawData);

      this.financialService.postFinancialCategpry(cleanPayload).subscribe({
        next: (res) => {
          this.toastService.success('Categoria Cadastrada com Sucesso.');
          this.saved.emit(res);
        },
        error: (err) => {
          console.error('Erro ao salvar:', err);
          this.isLoadingData = false;
        },
      });
    } else {
      this.validateForm(this.newFinancialCategory);
    }
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
    this.close.emit();
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
}
