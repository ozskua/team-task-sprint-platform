export interface Board {
  readonly boardId: number;
  readonly name: string;
  readonly areaId: number;

  readonly createdByUserId: number;
  readonly createdByFullName: string;

  readonly createdDate: string;
}

export interface Area {
  readonly areaId: number;
  readonly name: string;

  readonly createdByUserId: number;
  readonly createdByFullName: string;

  readonly createdDate: string;

  readonly boards: readonly Board[];
}

export interface CreateAreaRequest {
  readonly name: string;
}

export interface AreaWebhook {
  readonly webhookUrl: string | null;
}

export interface CreateBoardRequest {
  readonly name: string;
}
