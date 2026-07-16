/** Mirrors the backend PagedResponse<T> envelope (contracts/api.md pagination contract). */
export type PagedResponse<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}
