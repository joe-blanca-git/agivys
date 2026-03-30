//    =================================================================================================
//    SISTEMA.......: SIGAM-WEB
//    PROJETO.......: PORTAL SIGAM-WEB
//    OBJETO........: GERENCIADOR DO COMPONENTE NOTIFICATION TOAST .
//    RESPONSÁVEL...: WESLEY-BONINI
//    AUTOR.........: JOEDER-BLANCA
//    DATA..........: 00/00/00
//    =================================================================================================
//    PROPRIETÁRIO DO CÓDIGO FONTE: USINA ALTA MOGIANA S/A - AÇÚCAR E ÁLCOOL - COPYRIGHT - 2025
//    TODOS OS DIREITOS RESERVADOS. PROIBIDA A REPRODUÇÃO, DISTRIBUIÇÃO E QUALQUER USO NÃO AUTORIZADO 
//    DESTE CÓDIGO FONTE SEM AUTORIZAÇÃO ESCRITA.
//    =================================================================================================

import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export type ToastType = 'info' | 'success' | 'warning' | 'error';
export interface ToastConfig {
  message: string;
  type: ToastType;
  duration?: number;
}

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private toastSubject = new Subject<ToastConfig>();
  public toastState$ = this.toastSubject.asObservable();

  constructor() {}

  show(message: string, type: ToastType = 'info', duration: number = 4000): void {
    this.toastSubject.next({ message, type, duration });
  }

  success(message: string, duration?: number): void {
    this.show(message, 'success', duration);
  }

  error(message: string, duration?: number): void {
    this.show(message, 'error', duration);
  }

  info(message: string, duration?: number): void {
    this.show(message, 'info', duration);
  }

  warning(message: string, duration?: number): void {
    this.show(message, 'warning', duration);
  }
}