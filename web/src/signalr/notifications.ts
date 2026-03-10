import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

export enum NotificationType {
  SYSTEM = 'SYSTEM',
  ORDER_RECEIVED = 'ORDER_RECEIVED',
}

export type OrderReceivedNotificationPayload = {
  orderId: string;
  physicalFileId: string;
  message: string;
};

export type NotificationDto<TPayload = unknown> = {
  type: NotificationType;
  timestamp: string;
  payload?: TPayload;
  ackGuid?: string;
  ackRequired?: boolean;
};

export type NotificationHandler = (notification: NotificationDto) => void;

export class NotificationsService {
  private connection: HubConnection | null = null;
  private handlerProvider: (() => Iterable<NotificationHandler>) | null = null;
  private readonly seenAckGuids = new Set<string>();
  private readonly seenAckQueue: string[] = [];
  private readonly maxSeenAckGuids = 200;

  buildConnection(baseUrl?: string) {
    if (this.connection) {
      return;
    }

    const url = baseUrl
      ? `${baseUrl.replace(/\/$/, '')}/hubs/notifications`
      : '/hubs/notifications';

    this.connection = new HubConnectionBuilder()
      .withUrl(url, { withCredentials: true })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000, 40000, 80000])
      .configureLogging(LogLevel.Information)
      .build();

    this.connection.keepAliveIntervalInMilliseconds = 15000;
    this.connection.serverTimeoutInMilliseconds = 120000;

    this.connection.on('sendNotification', (notification: NotificationDto) => {
      if (notification.ackGuid != null) {
        if (this.seenAckGuids.has(notification.ackGuid)) {
          return;
        }

        this.seenAckGuids.add(notification.ackGuid);
        this.seenAckQueue.push(notification.ackGuid);
        if (this.seenAckQueue.length > this.maxSeenAckGuids) {
          const oldest = this.seenAckQueue.shift();
          if (oldest != null) {
            this.seenAckGuids.delete(oldest);
          }
        }
      }

      void this.sendAckIfRequired(notification);
      const handlers = this.handlerProvider?.() ?? [];
      for (const handler of handlers) {
        handler(notification);
      }
    });
  }

  setHandlerProvider(provider: () => Iterable<NotificationHandler>) {
    this.handlerProvider = provider;
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

  private async sendAckIfRequired(notification: NotificationDto) {
    if (!this.connection) {
      return;
    }

    if (!notification.ackRequired || notification.ackGuid == null) {
      return;
    }

    try {
      await this.connection.invoke('AckNotification', notification.ackGuid);
    } catch (error) {
      console.warn('Failed to send notification ack.', error);
    }
  }
}

export const notificationsService = new NotificationsService();
