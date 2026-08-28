export interface OrderReceivedNotificationPayload {
  orderId: string;
  physicalFileId: string;
  message: string;
}

export interface OrderSubmittedNotificationPayload {
  orderId: string;
  message: string;
}
