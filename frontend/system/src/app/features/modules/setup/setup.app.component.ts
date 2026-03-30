import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-setup.app',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './setup.app.component.html',
  styleUrls: ['./setup.app.component.scss', '../../../app.component.scss']
})
export class SetupAppComponent {

}
