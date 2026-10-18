import { Component, ChangeDetectionStrategy } from '@angular/core';
import { NavbarComponent } from '../navbar/navbar.component';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss'],
  imports: [NavbarComponent, RouterOutlet],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: true,
})
export class MainLayoutComponent {
  constructor() {}
}
