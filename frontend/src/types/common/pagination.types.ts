// Shared pagination envelope. Endpoints that page their results return this shape.

export interface Paginated<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
