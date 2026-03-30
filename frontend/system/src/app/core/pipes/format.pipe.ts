import { Pipe, PipeTransform, Inject, LOCALE_ID } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';

export type FormatType = 'currency' | 'date';

@Pipe({
  name: 'format',
  standalone: true,
})
export class FormatPipe implements PipeTransform {
  private currencyPipe: CurrencyPipe;
  private datePipe: DatePipe;

  constructor(@Inject(LOCALE_ID) private locale: string) {
    this.currencyPipe = new CurrencyPipe(this.locale);
    this.datePipe = new DatePipe(this.locale);
  }

  transform(
    value: string | number | Date | null | undefined,
    type: FormatType,
    options?: {
      currency?: string;
      dateFormat?: string;
    }
  ): string {
    if (value === null || value === undefined) {
      return '';
    }

    switch (type) {
      case 'currency': {
        // CurrencyPipe aceita apenas string | number
        if (typeof value !== 'number' && typeof value !== 'string') {
          return '';
        }

        return (
          this.currencyPipe.transform(
            value,
            options?.currency ?? 'BRL',
            'symbol',
            '1.2-2'
          ) ?? ''
        );
      }

      case 'date': {
        // DatePipe aceita Date | string | number
        return (
          this.datePipe.transform(
            value,
            options?.dateFormat ?? 'dd/MM/yyyy'
          ) ?? ''
        );
      }

      default:
        return String(value);
    }
  }
}
