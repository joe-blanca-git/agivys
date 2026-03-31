import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationService } from '../../../core/services/navigation.service';
import { RouterModule } from '@angular/router';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzIconModule } from 'ng-zorro-antd/icon';

@Component({
  selector: 'app-sider-menu',
  standalone: true,
  imports: [CommonModule, RouterModule, NzMenuModule, NzIconModule],
  templateUrl: './sider-menu.component.html',
  styleUrls: ['./sider-menu.component.scss'],
})
export class SiderMenuComponent {
  @Input() collapsed: boolean = false;

  menu: any = [];

  constructor(private menuService: NavigationService) {
    this.getMenu();
  }

  getMenu() {
    const menu = this.menuService.getMenuAllowed();
    
    if (menu.length > 0) {
      this.menu = menu;            
    } 
  }

}
