import { createOrderSubmittedHandler } from '@/signalr/handlers/orderSubmittedHandler';
import type { NotificationDto } from '@/signalr/notifications';
import type { OrderSubmittedNotificationPayload } from '@/signalr/payloads';
import { NotificationType } from '@/types/common';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('createOrderSubmittedHandler', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns early for non-order-submitted notifications', async () => {
    const fetchOrders = vi.fn();
    const orderService = {} as any;

    const handler = createOrderSubmittedHandler({
      orderService,
      ordersStore: { fetchOrders } as any,
    });

    const notification: NotificationDto<OrderSubmittedNotificationPayload> = {
      type: NotificationType.ORDER_RECEIVED,
      timestamp: new Date().toISOString(),
      payload: { orderId: 'order-1', message: '' },
    };

    await handler(notification);

    expect(fetchOrders).not.toHaveBeenCalled();
  });

  it('fetches orders for order-submitted notifications', async () => {
    const fetchOrders = vi.fn().mockResolvedValue([]);
    const orderService = {} as any;

    const handler = createOrderSubmittedHandler({
      orderService,
      ordersStore: { fetchOrders } as any,
    });

    const notification: NotificationDto<OrderSubmittedNotificationPayload> = {
      type: NotificationType.ORDER_SUBMITTED,
      timestamp: new Date().toISOString(),
      payload: { orderId: 'order-1', message: '' },
    };

    await handler(notification);

    expect(fetchOrders).toHaveBeenCalledTimes(1);
    expect(fetchOrders).toHaveBeenCalledWith(orderService);
  });

  it('awaits the fetchOrders promise', async () => {
    let resolveFetch: (() => void) | undefined;
    const fetchOrders = vi.fn(
      () =>
        new Promise<void>((resolve) => {
          resolveFetch = resolve;
        })
    );
    const orderService = {} as any;

    const handler = createOrderSubmittedHandler({
      orderService,
      ordersStore: { fetchOrders } as any,
    });

    const notification: NotificationDto<OrderSubmittedNotificationPayload> = {
      type: NotificationType.ORDER_SUBMITTED,
      timestamp: new Date().toISOString(),
      payload: { orderId: 'order-1', message: '' },
    };

    let settled = false;
    const handlerPromise = handler(notification).then(() => {
      settled = true;
    });

    expect(settled).toBe(false);

    resolveFetch?.();
    await handlerPromise;

    expect(settled).toBe(true);
  });
});
