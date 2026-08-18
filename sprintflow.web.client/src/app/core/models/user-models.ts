/**
 * One row of the Admin users list, mirroring the server's UserListItemDto.
 *
 * `roleName` is the global role and nothing more — it says nothing about Areas, Boards, Teams or
 * Tasks, and it is never used to make an authorization decision in the client.
 */
export interface UserListItem {
  readonly userId: number;
  readonly fullName: string;
  readonly email: string;
  readonly roleName: string;
  readonly isActive: boolean;

  /** ISO date-time without an offset, exactly as the server stored it. */
  readonly createdDate: string;
}
