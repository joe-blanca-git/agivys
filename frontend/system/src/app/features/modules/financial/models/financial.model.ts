export class FinancialOriginModel {
  id!: string;
  companyId!: string;
  description!: string;
  active!: boolean;
  createdAt!: string;
  createdBy!: string;
}

export class FinancialCategoryModel {
  id!: string;
  name!: string;
  type!: number;
  active!: boolean;
}

export class FinancialMovModel {
  id!: string;
  description!: string;
  totalAmount!: number;
  type!: number;
  documentDate!: string;
  categoryId!: string;
  categoryName!: string;
  accountId!: string;
  accountName!: string;
  personId!: string;
  personName!: string;
  originId!: string;
  originDescription!: string;
  createdAt!: string;
  installmentsCount!: number;
  installments!: Installments;
}

class Installments {
  number!: number;
  value!: number;
  dueDate!: string;
  status!: string;
}

export class PaymentSettle {
  message!: string;
  installmentsSettledCount!: number;
  totalAmountPaid!: number;
}

