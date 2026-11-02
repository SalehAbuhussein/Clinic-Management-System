export const LeaveStatus = {
  Pending: 0,
  Approved: 1,
  Rejected: 2,
} as const;

export type LeaveStatus = typeof LeaveStatus[keyof typeof LeaveStatus];

export type Leave = {
  id: number;
  fullname: string;
  from: string;
  to: string;
  reason: string;
  status: LeaveStatus;
  profileImage: string | null;
};

export type LeaveFilters = Partial<{
  status: `${LeaveStatus}`;
  from: string;
  to: string;
  page: number;
  perPage: number;
}>;

export type CreateLeaveRequest = {
  from: string;
  to: string;
  reason: string;
};

export type UpdateLeaveRequest = {
  reason: string;
};
