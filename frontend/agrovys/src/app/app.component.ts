import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  HostBinding,
  HostListener,
  OnInit,
} from '@angular/core';
import {
  NavigationEnd,
  Router,
  RouterModule,
  RouterOutlet,
} from '@angular/router';
import { filter } from 'rxjs';

// NG-ZORRO
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';

// PrimeNG
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { MenuItem } from 'primeng/api';

// Seus Componentes e Serviços
import { ScreenService } from './core/services/screen.service';
import { SiderMenuComponent } from './shared/components/sider-menu/sider-menu.component';
import { HeaderComponent } from './shared/components/header/header.component';
import { ToastContainerComponent } from './shared/components/toast-container/toast-container.component';
import { HeaderMenuComponent } from './shared/components/header-menu/header-menu.component';
import { AlertPageComponent } from './shared/components/alert-page/alert-page.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterModule,
    NzLayoutModule,
    NzMenuModule,
    NzIconModule,
    NzDrawerModule,
    BreadcrumbModule,
    SiderMenuComponent,
    HeaderComponent,
    HeaderMenuComponent,
    ToastContainerComponent,
    AlertPageComponent,
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  @HostBinding('class') get getClass() {
    return Object.keys(this.screen.sizes)
      .filter((cl) => this.screen.sizes[cl])
      .join(' ');
  }

  items: MenuItem[] | undefined;
  home: MenuItem | undefined;

  isMobile = false;
  isVisibleMenu = false;
  isVisibleAlert = true;
  isRouteReady = false;
  isPublicRoute = false;

  constructor(
    private screen: ScreenService,
    private router: Router,
  ) {
    this.initNavigationListener();
  }


  ngOnInit() {
    this.detectMobile();
    this.home = { icon: 'pi pi-home', routerLink: '/' };

    this.getUserLocation();
  }

  getStatusAlert(alerts: number){
    setTimeout(() => {
      this.isVisibleAlert = alerts !== 0; 
    });
  }

  private getUserLocation() {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          const coords = {
            lat: position.coords.latitude,
            lng: position.coords.longitude,
            timestamp: new Date().getTime(),
          };

          localStorage.setItem('user_location', JSON.stringify(coords));
        },
        (error) => {
          console.warn('Erro ao obter localização:', error.message);
        },
        {
          enableHighAccuracy: true,
          timeout: 5000,
          maximumAge: 0,
        },
      );
    } else {
      console.error('Geolocalização não é suportada por este navegador.');
    }
  }

  private initNavigationListener() {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        const publicRoutes = [
          '/auth/login',
          '/auth/recovery-password',
          '/auth',
        ];

        this.isPublicRoute = publicRoutes.some((route) =>
          event.urlAfterRedirects.startsWith(route),
        );

        if (this.isMobile) {
          this.isVisibleMenu = false;
        }

        this.isRouteReady = true;
      });
  }

  onChangeVisibleMenu(collapsed: boolean) {
    this.isVisibleMenu = collapsed;
  }

  @HostListener('window:resize')
  detectMobile(): void {
    const mobile = window.matchMedia('(max-width: 768px)').matches;

    if (this.isMobile !== mobile) {
      this.isMobile = mobile;
      this.isVisibleMenu = !mobile;
    }
  }
}
