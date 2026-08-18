/** A member of a team, derived by the server from task assignment. Never stored. */
export interface TeamMember {
  readonly userId: number;
  readonly fullName: string;
}

/**
 * One team. A team has no name of its own — it exists because a board does, one for one — so the
 * board and its area are what identify it.
 */
export interface Team {
  readonly teamId: number;
  readonly boardId: number;
  readonly boardName: string;
  readonly areaId: number;
  readonly areaName: string;

  /** The board's creator, inherited. Being the owner grants nothing and is not membership. */
  readonly ownerUserId: number;
  readonly ownerFullName: string;

  /** Empty is a normal state: a board whose tasks are all unassigned has nobody on its team. */
  readonly members: readonly TeamMember[];
}
