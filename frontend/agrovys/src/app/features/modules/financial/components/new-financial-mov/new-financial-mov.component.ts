import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  FormArray,
} from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { forkJoin, lastValueFrom, tap } from 'rxjs';
import { FormatPipe } from '../../../../../core/pipes/format.pipe';
import { PersonService } from '../../../../../shared/services/person.service';
import { FinancialService } from '../../services/financial.service';
import { AccountService } from '../../../../../shared/services/account.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { NewPersonFullComponent } from '../../../../../shared/components/new-person-full/new-person-full.component';
import { NewFinancialOriginComponent } from '../new-financial-origin/new-financial-origin.component';
import { NewFinancialCategoryComponent } from '../new-financial-category/new-financial-category.component';

interface Installment {
  number: number;
  dueDate: Date;
  value: number;
}

@Component({
  selector: 'app-new-financial-mov',
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
    NzTableModule,
    NzSwitchModule,
    NzAlertModule,
    NzSpinModule,
    ReactiveFormsModule,
    FormatPipe,
    NzDividerModule,
    NewPersonFullComponent,
    NewFinancialOriginComponent,
    NewFinancialCategoryComponent,
  ],
  providers: [FormatPipe],
  templateUrl: './new-financial-mov.component.html',
})
export class NewFinancialMovComponent implements OnInit {
  @Input() isVisible = false;
  @Input() movType = 0;
  @Input() title = 'Nova Movimentação';
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<any>();

  financialForm!: FormGroup;
  installments: Installment[] = [];
  listPerson: any[] = [];
  listOrigin: any[] = [];
  listCategory: any[] = [];
  listAccount: any[] = [];
  isLoadingData = true;
  isPersonModalVisible = false;
  isOriginModalVisible = false;
  isCategoryVisible = false;

  readonly nzFilterOption = () => true;

  constructor(
    private fb: FormBuilder,
    private personService: PersonService,
    private financialService: FinancialService,
    private accountService: AccountService,
    private toastService: ToastService,
  ) {}

  ngOnInit(): void {
    this.loadData();
    if (this.movType === 1) this.title = 'Nova Receita';
    if (this.movType === 2) this.title = 'Nova Despesa';
  }

  async loadData() {
    this.isLoadingData = true;
    try {
      await lastValueFrom(
        forkJoin({
          persons: this.personService
            .getPersonList('')
            .pipe(tap((d) => (this.listPerson = d))),
          origins: this.financialService
            .getFinancialOrign()
            .pipe(tap((d) => (this.listOrigin = d))),
          categories: this.financialService
            .getFinancialCategory()
            .pipe(
              tap(
                (d) =>
                  (this.listCategory = d.filter(
                    (i) => i.type === this.movType,
                  )),
              ),
            ),
          accounts: this.accountService
            .getAccountList()
            .pipe(tap((d) => (this.listAccount = d))),
        }),
      );
      this.initForm();
      this.watchChanges();
    } catch (error) {
      this.toastService.error('Erro ao carregar dados.');
    } finally {
      this.isLoadingData = false;
    }
  }

  private initForm() {
    this.financialForm = this.fb.group({
      description: ['', [Validators.required, Validators.minLength(3)]],
      type: [this.movType, [Validators.required]],
      totalAmount: [0, [Validators.required, Validators.min(0.01)]],
      installmentsCount: [1, [Validators.required, Validators.min(1)]],
      documentDate: [new Date(), [Validators.required]],
      firstDueDate: [new Date(), [Validators.required]],
      categoryId: [null],
      originId: ['6d790e02-fbd6-4d7d-90b9-e8163e2b3672', [Validators.required]],
      personId: [null, [Validators.required]],
      isPaid: [true],
      accountId: [this.listAccount[0]?.id],
      isInstallment: [true],
      paymentMethod: [null],
      paymentDate: [new Date()],
    });
  }

  private watchChanges() {
    this.financialForm.valueChanges.subscribe(() =>
      this.generateInstallments(),
    );

    this.financialForm.get('isPaid')?.valueChanges.subscribe((isPaid) => {
      const isInstallmentCtrl = this.financialForm.get('isInstallment');
      if (isPaid) {
        isInstallmentCtrl?.setValue(true, { emitEvent: true });
        isInstallmentCtrl?.disable();
      } else {
        isInstallmentCtrl?.enable();
      }
    });

    this.financialForm
      .get('isInstallment')
      ?.valueChanges.subscribe((isAtVista) => {
        const dateCtrl = this.financialForm.get('firstDueDate');
        const countCtrl = this.financialForm.get('installmentsCount');

        if (isAtVista) {
          dateCtrl?.setValue(new Date());
          countCtrl?.setValue(1);
        } else {
          if (!this.financialForm.get('isPaid')?.value) {
            dateCtrl?.setValue(this.addWorkingDays(new Date(), 30));
          }
        }
      });
  }

  private generateInstallments(): void {
    const { totalAmount, installmentsCount, firstDueDate } =
      this.financialForm.getRawValue();

    if (!totalAmount || !installmentsCount || !firstDueDate) {
      this.installments = [];
      return;
    }

    const baseValue = Math.floor((totalAmount / installmentsCount) * 100) / 100;
    const remainder = parseFloat(
      (totalAmount - baseValue * installmentsCount).toFixed(2),
    );

    this.installments = Array.from({ length: installmentsCount }, (_, i) => {
      const date = new Date(firstDueDate);
      date.setMonth(date.getMonth() + i);
      return {
        number: i + 1,
        dueDate: date,
        value:
          i === 0 ? parseFloat((baseValue + remainder).toFixed(2)) : baseValue,
      };
    });
  }

  handleOk() {
    if (this.financialForm.invalid)
      return this.validateForm(this.financialForm);
    this.isLoadingData = true;
    this.financialForm.disable();
    const rawData = this.financialForm.getRawValue();
    this.financialService
      .postFinancialMov(this.sanitizeData(rawData))
      .subscribe({
        next: (res) => {
          if (rawData.isPaid && res.id) {
            const body = {
              transactionId: res.id,
              accountId: rawData.accountId,
              totalAmountPaid: rawData.totalAmount,
              paymentDate: rawData.paymentDate,
              paymentMethod: rawData.paymentMethod,
            };
            this.financialService.postPaymentSettle(body).subscribe({
              next: () => this.finalizeSuccess(res),
              error: () => {
                this.isLoadingData = false;
                this.financialForm.enable();
              },
            });
          } else this.finalizeSuccess(res);
        },
        error: () => {
          this.isLoadingData = false;
          this.financialForm.enable();
        },
      });
  }

  private finalizeSuccess(res: any) {
    this.toastService.success('Sucesso.');
    this.saved.emit(res);
    this.isLoadingData = false;
    this.handleCancel();
  }

  private validateForm(form: any) {
    Object.values(form.controls).forEach((c: any) => {
      if (c instanceof FormGroup || c instanceof FormArray)
        this.validateForm(c);
      else {
        c.markAsDirty();
        c.updateValueAndValidity({ onlySelf: true });
      }
    });
  }

  private sanitizeData(data: any): any {
    if (typeof data !== 'object' || data === null)
      return data === '' ? null : data;
    Object.keys(data).forEach((k) => (data[k] = this.sanitizeData(data[k])));
    return data;
  }

  private addWorkingDays(date: Date, days: number): Date {
    let d = new Date(date),
      added = 0;
    while (added < days) {
      d.setDate(d.getDate() + 1);
      if (d.getDay() !== 0 && d.getDay() !== 6) added++;
    }
    return d;
  }

  handleCancel() {
    this.close.emit();
  }
  search(v: string) {}
  trackByItem = (i: number, item: any) => item.id || i;

  onOpenNewModal(m: string) {
    if (m === 'newPerson') this.isPersonModalVisible = true;
    else if (m === 'newOrigin') this.isOriginModalVisible = true;
    else if (m === 'newCategory') this.isCategoryVisible = true;
  }

  onCloseNewModal(m: string) {
    if (m === 'newPerson') this.isPersonModalVisible = false;
    else if (m === 'newOrigin') this.isOriginModalVisible = false;
    else if (m === 'newCategory') this.isCategoryVisible = false;
  }

  async onPersonCreated(p: any) {
    await lastValueFrom(
      this.personService
        .getPersonList('')
        .pipe(tap((d) => (this.listPerson = d))),
    );
    if (p?.id) this.financialForm.patchValue({ personId: p.id });
    this.onCloseNewModal('newPerson');
  }
  async onOriginCreated(o: any) {
    await lastValueFrom(
      this.financialService
        .getFinancialOrign()
        .pipe(tap((d) => (this.listOrigin = d))),
    );
    if (o?.id) this.financialForm.patchValue({ originId: o.id });
    this.onCloseNewModal('newOrigin');
  }
  async onCategoryCreated(c: any) {
    await lastValueFrom(
      this.financialService
        .getFinancialCategory()
        .pipe(
          tap(
            (d) =>
              (this.listCategory = d.filter((i) => i.type === this.movType)),
          ),
        ),
    );
    if (c?.id) this.financialForm.patchValue({ categoryId: c.id });
    this.onCloseNewModal('newCategory');
  }

  formatterReal = (v: any) =>
    v
      ? new Intl.NumberFormat('pt-BR', {
          style: 'currency',
          currency: 'BRL',
        }).format(typeof v === 'string' ? parseFloat(v) : v)
      : '';
  parserReal = (v: string) =>
    v
      .replace(/R\$\s?|(?:\s)/g, '')
      .replace(/\./g, '')
      .replace(',', '.');
}
