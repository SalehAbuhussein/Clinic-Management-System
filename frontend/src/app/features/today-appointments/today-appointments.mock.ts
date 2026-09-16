import { AppointmentCard } from '@shared/components/appointment-card/appointment-card.types';

/**
 * A plausible clinic morning, for building the list against before the API is
 * wired up. Every row exercises a different state.
 *
 * Synthetic: no real patient, doctor or record is represented here.
 * Delete once `AppointmentsService` returns live data.
 */

const DAY = '2026-09-16';

/**
 * Emit the exact shape the API sends: ISO 8601 with an explicit `+00:00`
 * offset and no milliseconds, not `toISOString()`'s `Z` form. Both parse to
 * the same instant, but matching the wire format means the screen is built
 * against what it will actually receive.
 */
const at = (time: string): string => `${DAY}T${time}:00+00:00`;

export const MOCK_APPOINTMENTS: AppointmentCard[] = [
  {
    id: 301,
    doctorId: 1,
    doctorName: 'Dr. Ahmad Saleh',
    patientId: 1042,
    patientName: 'Layla Haddad',
    startAt: at('08:00'),
    endAt: at('08:30'),
    status: 1,
    notes: null,
  },
  {
    id: 302,
    doctorId: 2,
    doctorName: 'Dr. Mona Darwish',
    patientId: 1043,
    patientName: 'Omar Nasser',
    startAt: at('08:00'),
    endAt: at('08:30'),
    status: 1,
    notes: 'Follow-up on last week’s bloodwork.',
  },
  {
    id: 303,
    doctorId: 1,
    doctorName: 'Dr. Ahmad Saleh',
    patientId: 1044,
    patientName: 'Rana Khalil',
    startAt: at('08:30'),
    endAt: at('09:00'),
    status: 1,
    notes: null,
  },
  {
    id: 304,
    doctorId: 3,
    doctorName: 'Dr. Karim Aziz',
    patientId: 1045,
    patientName: 'Yousef Mansour',
    startAt: at('08:30'),
    endAt: at('09:00'),
    status: 2,
    notes: 'Called twice, no answer.',
  },
  {
    id: 305,
    doctorId: 1,
    doctorName: 'Dr. Ahmad Saleh',
    patientId: 1048,
    patientName: 'Nadia Farouk',
    startAt: at('09:00'),
    endAt: at('09:30'),
    status: 3,
    notes: 'Cancelled by patient at 07:40.',
  },
  {
    id: 306,
    doctorId: 2,
    doctorName: 'Dr. Mona Darwish',
    patientId: 1047,
    patientName: 'Tariq Osman',
    startAt: at('09:00'),
    endAt: at('09:30'),
    status: 0,
    notes: null,
  },
  {
    id: 307,
    doctorId: 3,
    doctorName: 'Dr. Karim Aziz',
    patientId: 1046,
    patientName: 'Hana Bishara',
    startAt: at('09:00'),
    endAt: at('09:30'),
    status: 0,
    notes: null,
  },
  {
    id: 308,
    doctorId: 1,
    doctorName: 'Dr. Ahmad Saleh',
    patientId: 1049,
    patientName: 'Sami Wardeh',
    startAt: at('09:30'),
    endAt: at('10:00'),
    status: 0,
    notes: null,
  },
  {
    id: 309,
    doctorId: 2,
    doctorName: 'Dr. Mona Darwish',
    patientId: 1050,
    patientName: 'Dina Halabi',
    startAt: at('09:30'),
    endAt: at('10:15'),
    status: 0,
    notes: 'Long appointment, 45 minutes.',
  },
  {
    id: 310,
    doctorId: 3,
    doctorName: 'Dr. Karim Aziz',
    patientId: 1051,
    patientName: 'Fadi Zoubi',
    startAt: at('10:00'),
    endAt: at('10:30'),
    status: 0,
    notes: null,
  },
];
