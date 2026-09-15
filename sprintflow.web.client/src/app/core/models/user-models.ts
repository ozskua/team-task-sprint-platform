export interface UserListItem {
  readonly userId: number;
  readonly fullName: string;
  readonly email: string;
  readonly roleName: string;
  readonly isActive: boolean;

  readonly createdDate: string;
}
