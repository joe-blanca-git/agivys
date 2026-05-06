import { CommonModule, DecimalPipe } from '@angular/common';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'home-app-root',
  templateUrl: './home.app.component.html',
  styleUrls: ['./home.app.component.scss', '../../../app.component.scss'],
  standalone: true,
  imports: [CommonModule, RouterLink, DecimalPipe],
})
export class HomeAppComponent implements OnInit, OnDestroy {
  title = 'Página Inicial';
  description = '';
  formattedDate = '';

  farmsDashBoard: any = {};
  
  private timerInterval: any;

  ngOnInit(): void {
    console.log('ok');
    
    this.updateDate(); 
        this.timerInterval = setInterval(() => {
      this.updateDate();
    }, 1000);

    this.farmsDashBoard = {
      farms: 1200,
      fields: 21000,
      area: 120000
    }
  }

  ngOnDestroy(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
  }

  private updateDate(): void {
    const date = new Date();
    
    const dateString = date.toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
    
    this.formattedDate = `${dateString} - Brasil`;
  }
}