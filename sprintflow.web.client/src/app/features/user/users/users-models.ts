/**
 * A user as shown on the user-facing directory.
 *
 * Deliberately narrower than the spec's UserDto: Role, RoleId, IsActive and
 * CreatedDate are administrative details and are not exposed to normal users.
 * Only what is needed to recognise and contact someone.
 */
export interface UserSummary {
  /** Never rendered — used only as the list key. */
  readonly userId: number;
  readonly fullName: string;
  readonly email: string;
}
