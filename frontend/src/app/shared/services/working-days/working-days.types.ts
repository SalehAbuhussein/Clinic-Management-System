import { PagedResult } from "@core/types/api.types";

export type WorkingDay = {
  id: number;
  workingDay: string;
  startTime: string | null;
  endTime: string | null;
  isWorkingDay: boolean;
};

export type GetWorkingDaysResponse = PagedResult<WorkingDay>;
