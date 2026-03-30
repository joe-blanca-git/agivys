import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { UserPannelComponent } from '../user-pannel/user-pannel.component';
import { NotificationComponent } from "../notification/notification.component";

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, UserPannelComponent, NotificationComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss'],
})
export class HeaderComponent {
  @Output() collapsedOut: EventEmitter<boolean> = new EventEmitter<boolean>();

  collapsed = false;

  onChangeColapsed() {
    this.collapsed = !this.collapsed;

    this.collapsedOut.emit(this.collapsed);
  }
}
