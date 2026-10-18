export type AuditEntry = {
  id: number;
  occurredAt: string;
  actorId: number | null;
  actorName: string;
  action: string;
  targetType: string;
  targetId: number | null;
  detail: string;
};

export type AuditFilters = {
  from?: string;
  to?: string;
  page?: number;
  perPage?: number;
};
