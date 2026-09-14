import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { ThemeService } from '@core/services/theme/theme.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule],
  selector: 'app-theme-toggle',
  templateUrl: './theme-toggle.component.html',
})
export class ThemeToggleComponent {
  themeService = inject(ThemeService);
}
