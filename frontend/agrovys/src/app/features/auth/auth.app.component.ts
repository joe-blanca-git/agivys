import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { RouterOutlet } from "@angular/router";

@Component({
    selector: 'auth-app-root',
    templateUrl: './auth.app.component.html',
    styleUrls: ['./auth.app.component.scss'],
    standalone: true,
    imports: [
        CommonModule,
        RouterOutlet
    ]
    
})
export class AuthAppComponent {
    
}