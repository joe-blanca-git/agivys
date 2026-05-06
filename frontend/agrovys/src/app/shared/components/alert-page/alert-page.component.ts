import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-alert-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alert-page.component.html',
  styleUrl: './alert-page.component.scss',
})
export class AlertPageComponent implements OnInit {
  @Output() statusAlert = new EventEmitter<number>();

  messages: any[] = [];

  ngOnInit(): void {
    this.loadData();
  }

  loadData() {
    this.messages = [
      // {
      //   id: 1,
      //   text: `Atenção! Seu Teste Gratuito expira dia 09/04/2026 - <a href="" class="text-primary">CLIQUE AQUI</a> para renovar seu plano.`,
      //   type: 'warning',
      //   show: true,
      // },
    ];

    this.emitQtyAlerts();
  }

  emitQtyAlerts() {
    const activeCount = this.messages.filter((alert) => alert.show).length;
    this.statusAlert.emit(activeCount);
  }

  closeAlertById(id: number) {
    this.messages = this.messages.map((m) => {
      if (m.id === id) m.show = false;
      return m;
    });

    const count = this.messages.filter((m) => m.show).length;
    this.statusAlert.emit(count);
  }
}
