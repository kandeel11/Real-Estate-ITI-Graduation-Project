import { Component, inject, output } from '@angular/core';
import { LanguageService } from '../../../../../core/services/language.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [],
  templateUrl: './topbar.html',
  styleUrls: ['./topbar.scss'],
})
export class TopbarComponent {
  menuToggle = output<void>();
  protected readonly lang = inject(LanguageService);
}
