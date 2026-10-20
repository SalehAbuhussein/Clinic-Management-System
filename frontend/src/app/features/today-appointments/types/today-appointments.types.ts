/**
 * What the dialog closes with. Each one opens another dialog or a confirmation,
 * so the screen owns them rather than stacking two on top of each other.
 */
export type AppointmentDetailAction = 'checkIn' | 'reschedule' | 'noShow' | 'cancel' | 'openVisit';
