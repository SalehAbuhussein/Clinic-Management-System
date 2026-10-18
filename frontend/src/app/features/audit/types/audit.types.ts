import { AuditEntry } from '@shared/services/audit/audit.types';

/** How far back the list reaches. `all` drops the filter entirely. */
export type AuditPreset = '1' | '7' | '30' | 'all';

/** What an action reads as on screen, and the icon beside it. */
export type ActionWording = {
  label: string;
  icon: string;
};

/** A stored entry dressed for the template. */
export type AuditRow = AuditEntry & ActionWording & {
  actor: string;
};
