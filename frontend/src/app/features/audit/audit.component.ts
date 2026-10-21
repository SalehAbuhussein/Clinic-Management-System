import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

import { AuditService } from '@shared/services/audit/audit.service';

import { dayRange } from '@core/utils/day.utils';

import { ExportButtonComponent } from '@shared/components/export-button/export-button.component';
import { AuditDetailDialogComponent } from './components/audit-detail-dialog/audit-detail-dialog.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';

import { MatDialog } from '@angular/material/dialog';

import { ActionWording, AuditPreset, AuditRow } from './types/audit.types';

@Component({
  imports: [DatePipe, ExportButtonComponent, MatButtonToggle, MatButtonToggleGroup, MatIcon, MatProgressSpinner, PaginationComponent],
  selector: 'app-audit',
  styleUrl: './audit.component.scss',
  templateUrl: './audit.component.html',
})
export class AuditComponent {
  auditService = inject(AuditService);
  dialog = inject(MatDialog);

  /**
   * What each action reads as on screen, and the icon beside it. The API
   * stores the key, so an action added later shows its raw name rather than
   * breaking the row.
   */
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

  preset = signal<AuditPreset>('7');
  page = signal(1);

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

  total = computed(() => this.auditResource.value().total);
  perPage = computed(() => this.auditResource.value().perPage || 10);

  /**
   * The rows as the template renders them: the stored action replaced by its
   * wording and icon, and a null actor named rather than left blank.
   */
  entries = computed<AuditRow[]>(() => this.auditResource.value().items.map(entry => ({
    ...entry,
    ...(this.ACTION_WORDING[entry.action] ?? { label: entry.action, icon: 'help' }),
    actor: entry.actorName || 'Someone unidentified',
  })));

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
    this.page.set(1);
  }
}
