/**
 * A board inside an Area, created and named by a user. The server also creates the board's one
 * team, which has no client-facing representation.
 */
export interface Board {
  readonly boardId: number;
  readonly name: string;
  readonly areaId: number;

  /** Recorded ownership, inherited from the Area's creator. It grants no permission. */
  readonly createdByUserId: number;
  readonly createdByFullName: string;

  /** ISO date-time without an offset, exactly as the server stored it. */
  readonly createdDate: string;
}

/**
 * An Area as the server returns it. Every field comes from the API — the client never fills any of
 * them in itself.
 *
 * A newly created Area has no boards: boards are added afterwards, by a user who names them.
 */
export interface Area {
  readonly areaId: number;
  readonly name: string;

  /** Recorded ownership. It grants the creator no permission over the Area. */
  readonly createdByUserId: number;
  readonly createdByFullName: string;

  /** ISO date-time without an offset, exactly as the server stored it. */
  readonly createdDate: string;

  readonly boards: readonly Board[];
}

/** The only thing a client may supply. The creator comes from the token, never from here. */
export interface CreateAreaRequest {
  readonly name: string;
}

/** Same rule for boards: the name only. The Area comes from the route, the creator from the token. */
export interface CreateBoardRequest {
  readonly name: string;
}
