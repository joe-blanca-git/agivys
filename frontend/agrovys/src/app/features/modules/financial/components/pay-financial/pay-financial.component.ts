import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { FormatPipe } from '../../../../../core/pipes/format.pipe';
import { FinancialService } from '../../services/financial.service';
import { AccountService } from '../../../../../shared/services/account.service';
import { ToastService } from '../../../../../core/services/toast.service';

export interface IFiancialPay {
  categoryName: string;
  description: string;
  dueDate: string;
  isOverdue: boolean;
  number: number;
  personName: string;
  status: string;
  totalInstallments: number;
  value: number;
}

@Component({
  selector: 'app-pay-financial',
  standalone: true,
  imports: [
    CommonModule,
    NzButtonModule,
    NzModalModule,
    NzFormModule,
    NzSelectModule,
    NzInputNumberModule,
    NzInputModule,
    NzDatePickerModule,
    NzTagModule,
    NzSpinModule,
    ReactiveFormsModule,
    FormatPipe,
    NzDividerModule,
  ],
  templateUrl: './pay-financial.component.html',
  styleUrl: './pay-financial.component.scss',
})
export class PayFinancialComponent implements OnInit, OnChanges {
  @Input() isVisible = false;
  @Input() movType = 0;
  @Input() title = 'Detalhes da Conta';
  @Input() financialPay?: IFiancialPay;
  @Output() close = new EventEmitter<boolean>();
  @Output() saved = new EventEmitter<boolean>();

  installmentsForm!: FormGroup;
  listAccount: any[] = [];
  isLoadingData = false;

  constructor(
    private fb: FormBuilder,
    private financialService: FinancialService,
    private accountService: AccountService,
    private toastService: ToastService,
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    //this.loadAccounts();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isVisible']?.currentValue && this.isVisible) {
      this.updateUI();
    }
  }

  private initForm(): void {
    this.installmentsForm = this.fb.group({
      paymentDate: [new Date(), [Validators.required]],
      paymentMethod: [null, [Validators.required]],
      amountPaid: [0, [Validators.required, Validators.min(0.01)]],
      accountId: [null, [Validators.required]],
    });
  }

  private updateUI(): void {
    if (this.movType === 1) this.title = 'Registrar Recebimento';
    if (this.movType === 2) this.title = 'Registrar Pagamento';

    if (this.financialPay) {
      this.installmentsForm.patchValue({
        amountPaid: this.financialPay.value,
        paymentDate: new Date(),
        accountId: this.listAccount.length > 0 ? this.listAccount[0].id : null
      });
    }
  }

  // No loadAccounts, certifique-se que o spin não prenda o form se a lista vier vazia
private loadAccounts(): void {
  this.isLoadingData = true;
  this.accountService.getAccountList().subscribe({
    next: (res) => {
      this.listAccount = res || [];
      if (this.listAccount.length > 0) {
        this.installmentsForm.get('accountId')?.setValue(this.listAccount[0].id);
      }
    },
    error: () => this.toastService.error('Erro ao carregar contas'),
    complete: () => {
      this.isLoadingData = false;
      this.updateUI(); // Garante a atualização após carregar as contas
    }
  });
}

  getStatusColor(status?: string, isOverdue?: boolean): string {
    if (isOverdue) return 'red';
    return status === 'Paid' ? 'green' : 'gold';
  }

  handleOk(): void {
    if (this.installmentsForm.invalid) {
      Object.values(this.installmentsForm.controls).forEach(c => {
        c.markAsDirty();
        c.updateValueAndValidity();
      });
      return;
    }

    this.isLoadingData = true;
    this.financialService.postPaymentSettle(this.installmentsForm.value).subscribe({
      next: () => {
        this.toastService.success('Operação realizada!');
        this.saved.emit(true);
        this.handleCancel();
      },
      error: () => this.toastService.error('Erro ao salvar'),
      complete: () => this.isLoadingData = false
    });
  }

  handleCancel(): void {
    this.close.emit(true);
  }

  trackByItem = (i: number, item: any) => item.id || i;
}