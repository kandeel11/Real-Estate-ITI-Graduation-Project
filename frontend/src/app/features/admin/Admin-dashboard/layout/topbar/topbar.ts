import { Component, inject, output } from '@angular/core';
import { LanguageService } from '../../../../../core/services/language.service';
import { AuthService } from '../../../../../core/services/auth.service';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './topbar.html',
  styleUrls: ['./topbar.scss'],
})
export class TopbarComponent {
  menuToggle = output<void>();
  protected readonly lang  = inject(LanguageService);
  protected readonly auth  = inject(AuthService);
}
