export type LeaveRequestData = {
  mode: 'day' | 'range';
  from: Date | null;
  to: Date | null;
  day: Date | null;
  startTime: string;
  endTime: string;
  reason: string;
};

export type LeaveDetailAction = 'approve' | 'reject';
