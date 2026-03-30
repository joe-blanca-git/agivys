import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { ToastService, ToastType } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  styleUrl: './toast-container.component.scss',
  template: `
    <div
      class="toast-container position-fixed top-0 end-0 p-3"
      style="z-index: 1200"
    >
      <div
        class="toast align-items-center text-white border-0 show"
        role="alert"
        aria-live="assertive"
        aria-atomic="true"
        *ngIf="isVisible"
        [ngClass]="toastClass"
      >
        <div class="d-flex align-items-center">
          <div class="toast-icon ps-3">
            <i [ngClass]="toastIconClass"></i>
          </div>
          <div class="toast-body">
            {{ message }}
          </div>
          <button
            type="button"
            class="btn-close btn-close-white me-2 m-auto"
            aria-label="Close"
            (click)="hide()"
          ></button>
        </div>
      </div>
    </div>
  `,
})
export class ToastContainerComponent implements OnInit, OnDestroy {
  isVisible = false;
  message = '';
  type: ToastType = 'info';
  duration = 4000;

  private timeoutId: any;
  private subscription: Subscription = new Subscription();

  constructor(private toastService: ToastService) {}

  ngOnInit(): void {
    this.subscription = this.toastService.toastState$.subscribe(
      (toastConfig) => {
        this.message = toastConfig.message;
        this.type = toastConfig.type;

        if (toastConfig.duration) {
          this.duration = toastConfig.duration;
        }

        this.show();
      }
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    clearTimeout(this.timeoutId);
  }

  show(): void {
    this.isVisible = true;
    clearTimeout(this.timeoutId);

    this.timeoutId = setTimeout(() => {
      this.hide();
    }, this.duration);
  }

  hide(): void {
    this.isVisible = false;
  }

  get toastClass(): string {
    switch (this.type) {
      case 'success':
        return 'bg-success';
      case 'error':
        return 'bg-danger';
      case 'warning':
        return 'bg-warning text-danger';
      case 'info':
      default:
        return 'bg-primary';
    }
  }

  get toastIconClass(): string {
    switch (this.type) {
      case 'success':
        return 'fa fa-check';
      case 'error':
        return 'fa fa-xmark';
      case 'warning':
        return 'fa fa-exclamation-triangle text-danger';
      case 'info':
      default:
        return 'fa fa-info-circle';
    }
  }
}
