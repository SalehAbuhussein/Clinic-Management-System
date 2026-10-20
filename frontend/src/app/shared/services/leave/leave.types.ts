export type Leave = {
  id: number;
  fullname: string;
  from: string;
  to: string;
  reason: string;
  status: number;
};

export type LeaveFilters = Partial<{
  status: string;
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
