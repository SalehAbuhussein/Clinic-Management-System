export type PagedResult<T> = {
  items: Array<T>;
  page: number;
  perPage: number;
  total: number;
};
