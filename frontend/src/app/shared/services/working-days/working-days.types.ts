import { PagedResult } from "@core/types/api.types";

export type WorkingDay = {
  id: number;
  workingDay: string;
  startAt: string | null;
  endAt: string | null;
  isWorkingDay: boolean;
};

export type GetWorkingDaysResponse = PagedResult<WorkingDay>;
