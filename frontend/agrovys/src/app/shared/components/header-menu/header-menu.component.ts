import { Component } from '@angular/core';
import { Router, RouterLinkActive, RouterLink } from '@angular/router';

@Component({
  selector: 'app-header-menu',
  standalone: true,
  imports: [RouterLinkActive, RouterLink],
  templateUrl: './header-menu.component.html',
  styleUrl: './header-menu.component.scss',
})
export class HeaderMenuComponent {
  constructor(private router: Router) {}
}
