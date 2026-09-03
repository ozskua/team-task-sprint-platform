import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

import { environment } from '../../../environments/environment';
import { BoardDetailService } from './board-detail.service';
import { TokenStorage } from './token-storage';

/**
 * Keeps the board on screen in step with what other people are doing to it.
 *
 * The connection is the whole of this class's job: opening it, joining the group for the board
 * being viewed, leaving it again, and re-reading the board when the server says it changed. It
 * decides nothing — the REST API remains the only way anything is written, and the only thing this
 * does on an event is call the reload that already existed.
 *
 * Realtime is an improvement on the page, never a requirement for it. Every failure here is
 * swallowed after one warning: if the hub cannot be reached the board still loads over REST, and
 * the user sees a board that updates when they act rather than one that is broken.
 */
@Injectable()
export class BoardRealtimeService {
  private readonly boardDetail = inject(BoardDetailService);
  private readonly tokenStorage = inject(TokenStorage);

  private connection: HubConnection | null = null;

  /** The board whose group this connection is in, so a repeated watch of it does nothing. */
  private joinedBoardId: number | null = null;

  /** Serialises start/join/leave, so a fast board switch cannot interleave two of them. */
  private queue: Promise<void> = Promise.resolve();

  /** Said once per connection rather than on every retry, which is what would fill the console. */
  private warned = false;

  /**
   * Watch one board. Called whenever the route's board id changes, including the first time.
   *
   * Switching boards leaves the old group before joining the new one — without that, a change on
   * the board just left would still arrive and reload the board now on screen.
   */
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

  /** Leaves the group and closes the connection. Called when the board page goes away. */
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

    // Signed out: there is nothing to authenticate with, and the hub would refuse the connection.
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
        // Read fresh on every attempt, so a reconnect after signing in again uses the new token.
        accessTokenFactory: () => this.tokenStorage.read()?.token ?? ''
      })
      .withAutomaticReconnect()
      // Silenced because the client logs every failed retry at Error level, and a backend that is
      // simply not running would fill the console with them. This class says it once instead, in
      // warnOnce, which is the only message worth reading here.
      .configureLogging(LogLevel.Critical)
      .build();

    connection.on('boardChanged', () => this.reloadWatchedBoard());

    // A reconnect starts a new connection with no groups, so the board has to be rejoined and
    // re-read: whatever changed while the connection was down was never delivered.
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

  /**
   * Re-reads the board through the service that already owns it, which keeps whichever view is
   * open — the kanban's sprint scope or the backlog's full inventory.
   */
  private reloadWatchedBoard(): void {
    const boardId = this.joinedBoardId;

    if (boardId === null) {
      return;
    }

    this.boardDetail.reload(boardId).subscribe({
      // A failed refresh leaves the board as it was, which is better than clearing it.
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

  /** Every hub call is best-effort: a failure must not reach the page or repeat in the console. */
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
