import { Component, output } from '@angular/core';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [],
  templateUrl: './topbar.html',
  styleUrls: ['./topbar.scss'],
})
export class TopbarComponent {
  menuToggle = output<void>();
}
