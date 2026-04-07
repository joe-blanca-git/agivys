import { CommonModule } from '@angular/common';
import {
  Component,
  ContentChild,
  EventEmitter,
  Input,
  Output,
  TemplateRef,
  OnChanges,
  SimpleChanges,
} from '@angular/core';

export interface ModalAction {
  class: string;
  type: string;
  icon?: string;
  text: string;
  title?: string;
}

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss',
})
export class ModalComponent implements OnChanges {
  @Input() isVisible: boolean = false;
  @Input() isCentered: boolean = false;

  @Input() isHeader: boolean = true;
  @Input() title: string = 'Título do Modal';
  @Input() iconTitle: string = '';
  @Input() styleTextTitle: string = '';
  @Input() description: string = 'Descrição do Modal';

  @Input() isActionsDisabled: boolean = false;

  @Input() size: 'sm' | 'md' | 'lg' | 'xl' | null = null;

  @Input() customActions: ModalAction[] = [];

  @ContentChild(TemplateRef) contentTemplate!: TemplateRef<any>;

  @Output() statusModal = new EventEmitter<boolean>();
  @Output() actionClick = new EventEmitter<string>();
  @Output() close = new EventEmitter<boolean>();

  zIndexBackdrop: number = 1050;
  zIndexModal: number = 1055;

  ngOnChanges(changes: SimpleChanges) {
    if (changes['isVisible'] && changes['isVisible'].currentValue === true) {
      this.calcZIndex();
    }
  }

  calcZIndex() {
    const modaisAbertos = document.querySelectorAll('.modal.show').length;
    this.zIndexBackdrop = 1050 + modaisAbertos * 10;
    this.zIndexModal = 1055 + modaisAbertos * 10;
  }

  openModal(options?: {
    centered?: boolean;
    size?: 'sm' | 'md' | 'lg' | 'xl';
  }) {
    this.isCentered = options?.centered ?? false;
    this.size = options?.size ?? null;
    this.calcZIndex();
    this.isVisible = true;
  }

  closeModal() {
    (document.activeElement as HTMLElement)?.blur();
    this.close.emit(false);
  }

  onActionClick(type: string) {
    this.actionClick.emit(type);
  }
}
