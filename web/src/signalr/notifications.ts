import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

export type NotificationDto = {
  type: string;
  message: string;
  timestamp: string;
  payload?: unknown;
};

type NotificationHandler = (notification: NotificationDto) => void;

export class NotificationsService {
  private connection: HubConnection | null = null;
  private handlers: NotificationHandler[] = [];

  buildConnection(baseUrl?: string) {
    if (this.connection) {
      return;
    }

    const url = baseUrl
      ? `${baseUrl.replace(/\/$/, '')}/hubs/notifications`
      : '/hubs/notifications';

    this.connection = new HubConnectionBuilder()
      .withUrl(url, { withCredentials: true })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(LogLevel.Information)
      .build();

    this.connection.on(
      'notificationReceived',
      (notification: NotificationDto) => {
        this.handlers.forEach((handler) => handler(notification));
      }
    );
  }

  onNotification(handler: NotificationHandler) {
    this.handlers.push(handler);
  }

  async start() {
    if (!this.connection) {
      this.buildConnection();
    }

    if (!this.connection) {
      return;
    }

    if (this.connection.state === HubConnectionState.Connected) {
      return;
    }

    await this.connection.start();
  }

  async stop() {
    if (
      this.connection &&
      this.connection.state !== HubConnectionState.Disconnected
    ) {
      await this.connection.stop();
    }
  }
}

export const notificationsService = new NotificationsService();
