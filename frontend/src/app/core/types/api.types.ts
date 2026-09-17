export type PagedResponse<T> = {
  items: Array<T>;
  page: number;
  perPage: number;
  total: number;
};
