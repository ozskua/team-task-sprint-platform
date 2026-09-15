import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

import { environment } from '../../../environments/environment';
import { BoardDetailService } from './board-detail.service';
import { TokenStorage } from './token-storage';

@Injectable()
export class BoardRealtimeService {
  private readonly boardDetail = inject(BoardDetailService);
  private readonly tokenStorage = inject(TokenStorage);

  private connection: HubConnection | null = null;

  private joinedBoardId: number | null = null;

  private queue: Promise<void> = Promise.resolve();

  private warned = false;

  watch(boardId: number): void {
    if (!Number.isInteger(boardId) || boardId <= 0 || boardId === this.joinedBoardId) {
      return;
    }

    this.enqueue(async () => {
      await this.leaveCurrent();

      const connection = await this.ensureStarted();

      if (connection === null) {
        return;
      }

      await connection.invoke('JoinBoard', boardId);
      this.joinedBoardId = boardId;
    });
  }

  stop(): void {
    this.enqueue(async () => {
      await this.leaveCurrent();

      const connection = this.connection;
      this.connection = null;
      this.warned = false;

      await connection?.stop();
    });
  }

  private async ensureStarted(): Promise<HubConnection | null> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return this.connection;
    }

    const token = this.tokenStorage.read()?.token;

    if (token === undefined) {
      return null;
    }

    if (this.connection === null) {
      this.connection = this.build();
    }

    try {
      if (this.connection.state === HubConnectionState.Disconnected) {
        await this.connection.start();
      }

      return this.connection.state === HubConnectionState.Connected ? this.connection : null;
    } catch {
      this.warnOnce();

      return null;
    }
  }

  private build(): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl(environment.boardHubUrl, {
        accessTokenFactory: () => this.tokenStorage.read()?.token ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Critical)
      .build();

    connection.on('boardChanged', () => this.reloadWatchedBoard());

    connection.onreconnected(() => {
      const boardId = this.joinedBoardId;

      if (boardId === null) {
        return;
      }

      this.enqueue(async () => {
        await connection.invoke('JoinBoard', boardId);
        this.reloadWatchedBoard();
      });
    });

    return connection;
  }

  private reloadWatchedBoard(): void {
    const boardId = this.joinedBoardId;

    if (boardId === null) {
      return;
    }

    this.boardDetail.reload(boardId).subscribe({
      error: () => this.warnOnce()
    });
  }

  private async leaveCurrent(): Promise<void> {
    const boardId = this.joinedBoardId;
    this.joinedBoardId = null;

    if (boardId === null || this.connection?.state !== HubConnectionState.Connected) {
      return;
    }

    await this.connection.invoke('LeaveBoard', boardId);
  }

  private enqueue(work: () => Promise<void>): void {
    this.queue = this.queue
      .then(work)
      .catch(() => this.warnOnce());
  }

  private warnOnce(): void {
    if (this.warned) {
      return;
    }

    this.warned = true;
    console.warn('Live board updates are unavailable. The board still loads and saves normally.');
  }
}
