export interface TeamMember {
  readonly userId: number;
  readonly fullName: string;
}

export interface Team {
  readonly teamId: number;
  readonly boardId: number;
  readonly boardName: string;
  readonly areaId: number;
  readonly areaName: string;

  readonly ownerUserId: number;
  readonly ownerFullName: string;

  readonly members: readonly TeamMember[];
}
