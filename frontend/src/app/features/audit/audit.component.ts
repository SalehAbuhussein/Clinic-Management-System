import { ActivatedRoute, Router } from '@angular/router';
import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

import { AuditService } from '@shared/services/audit/audit.service';

import { dayRange } from '@core/utils/day.utils';
import { pageFromUrl, syncUrl } from '@core/utils/url-state.utils';

import { AuditDetailDialogComponent } from './components/audit-detail-dialog/audit-detail-dialog.component';
import { ExportButtonComponent } from '@shared/components/export-button/export-button.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';

import { MatDialog } from '@angular/material/dialog';

import { ActionWording, AuditPreset, AuditRow } from './types/audit.types';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ExportButtonComponent, MatButtonToggle, MatButtonToggleGroup, MatIcon, MatProgressSpinner, PaginationComponent],
  selector: 'app-audit',
  styleUrl: './audit.component.scss',
  templateUrl: './audit.component.html',
})
export class AuditComponent {
  auditService = inject(AuditService);
  dialog = inject(MatDialog);
  route = inject(ActivatedRoute);
  router = inject(Router);

  readonly ACTION_WORDING: Record<string, ActionWording> = {
    LoginSucceeded: { label: 'signed in', icon: 'login' },
    LoginFailed: { label: 'failed to sign in', icon: 'gpp_maybe' },
    RefreshRejected: { label: 'had a session refresh rejected', icon: 'gpp_maybe' },
    UserCreated: { label: 'created a user', icon: 'person_add' },
    UserRoleChanged: { label: 'changed a role', icon: 'manage_accounts' },
    UserDeactivated: { label: 'deactivated a user', icon: 'person_off' },
    UserActivated: { label: 'activated a user', icon: 'how_to_reg' },
    PasswordChanged: { label: 'changed their password', icon: 'password' },
    PasswordReset: { label: 'reset a password', icon: 'lock_reset' },
    AccessDenied: { label: 'was refused access', icon: 'block' },
  };

  readonly PRESETS: readonly AuditPreset[] = ['1', '7', '30', 'all'];

  preset = signal<AuditPreset>(this.presetFromUrl());

  page = linkedSignal<AuditPreset, number>({
    source: this.preset,
    computation: (_, previous) => previous ? 1 : pageFromUrl(this.route),
  });

  from = computed(() => {
    const preset = this.preset();

    if (preset === 'all') {
      return undefined;
    }

    const daysCovered = Number(preset);
    const firstDay = new Date();

    firstDay.setDate(firstDay.getDate() - (daysCovered - 1));

    return dayRange(firstDay).from;
  });

  exportFilters = computed(() => ({ from: this.from() }));

  auditResource = this.auditService.getAuditResource(() => ({ from: this.from(), page: this.page() }));

  /**
   * The rows as the template renders them: the stored action replaced by its
   * wording and icon, and a null actor named rather than left blank.
   */
  entries = computed<AuditRow[]>(() => this.auditResource.value().items.map(entry => ({
    ...entry,
    ...(this.ACTION_WORDING[entry.action] ?? { label: entry.action, icon: 'help' }),
    actor: entry.actorName || 'Someone unidentified',
  })));

  constructor() {
    syncUrl(this.router, this.route, () => ({ preset: this.preset(), page: this.page() }));
  }

  /**
   * Show everything stored about one entry, including the target and the raw
   * action key, which the row itself softens into a sentence.
   *
   * @param entry The row that was picked.
   */
  openDetail(entry: AuditRow): void {
    this.dialog.open(AuditDetailDialogComponent, { width: '460px', data: entry });
  }

  /**
   * Narrow or widen the window. The page resets, because page 4 of a week
   * rarely exists in a day.
   *
   * @param preset The range that was picked.
   */
  selectPreset(preset: AuditPreset): void {
    this.preset.set(preset);
  }

  /**
   * The range the URL asked for. Anything else is ignored rather than trusted,
   * because an unknown preset would parse to NaN and the window would collapse
   * to an invalid date.
   *
   * @returns A range the screen knows, defaulting to the last seven days.
   */
  presetFromUrl(): AuditPreset {
    const asked = this.route.snapshot.queryParamMap.get('preset') as AuditPreset | null;

    return asked && this.PRESETS.includes(asked) ? asked : '7';
  }
}
