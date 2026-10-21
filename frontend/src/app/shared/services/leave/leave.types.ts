/** 0 Pending, 1 Approved, 2 Rejected, as the API stores them. */
export type LeaveStatus = 0 | 1 | 2;

export type Leave = {
  id: number;
  fullname: string;
  from: string;
  to: string;
  reason: string;
  status: LeaveStatus;
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
