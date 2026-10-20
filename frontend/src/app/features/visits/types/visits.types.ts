/**
 * What the dialog closes with. The screen owns every one of these, because each
 * opens another dialog and two stacked dialogs read badly.
 */
export type VisitDetailAction = 'diagnose' | 'prescribe' | 'edit' | 'checkout' | 'delete';
