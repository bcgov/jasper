import { createOrderReceivedHandler } from '@/signalr/handlers/orderReceivedHandler';
import type { NotificationDto } from '@/signalr/notifications';
import type { OrderReceivedNotificationPayload } from '@/signalr/payloads';
import type { Order } from '@/types';
import { NotificationType, OrderReviewStatus } from '@/types/common';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const { getCourtClassLabelMock } = vi.hoisted(() => ({
  getCourtClassLabelMock: vi.fn(),
}));

vi.mock('@/utils/utils', () => ({
  getCourtClassLabel: getCourtClassLabelMock,
}));

describe('createOrderReceivedHandler', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getCourtClassLabelMock.mockReturnValue('Criminal');
  });

  it('returns early for non-order-received notifications', async () => {
    const fetchOrders = vi.fn();
    const showSnackbar = vi.fn();
    const viewOrderDetails = vi.fn();
    const viewOrders = vi.fn();

    const handler = createOrderReceivedHandler({
      orderService: {} as any,
      ordersStore: { fetchOrders } as any,
      snackbarStore: { showSnackbar } as any,
      viewOrderDetails,
      viewOrders,
    });

    const notification: NotificationDto<OrderReceivedNotificationPayload> = {
      type: 'UNKNOWN' as NotificationType,
      timestamp: new Date().toISOString(),
      payload: { orderId: 'order-1', physicalFileId: 'file-1', message: '' },
    };

    await handler(notification);

    expect(fetchOrders).not.toHaveBeenCalled();
    expect(showSnackbar).not.toHaveBeenCalled();
  });

  it('shows snackbar and invokes view callback when order is found', async () => {
    const order: Order = {
      id: 'order-1',
      packageId: 12345,
      priorityType: 'P1',
      priorityTypeDescription: 'High',
      courtListType: 'Trial List',
      packageDocumentId: 'doc-1',
      packageName: 'Order Package',
      receivedDate: '2026-01-01',
      processedDate: '2026-01-02',
      courtClass: 'CC',
      courtFileNumber: '12345-1',
      styleOfCause: 'R v Smith',
      physicalFileId: 'file-1',
      status: OrderReviewStatus.Pending,
    };

    const orderService = { getOrders: vi.fn() } as any;
    const fetchOrders = vi.fn().mockResolvedValue([order]);
    const showSnackbar = vi.fn();
    const viewOrderDetails = vi.fn();

    const handler = createOrderReceivedHandler({
      orderService,
      ordersStore: { fetchOrders } as any,
      snackbarStore: { showSnackbar } as any,
      viewOrderDetails,
    });

    const notification: NotificationDto<OrderReceivedNotificationPayload> = {
      type: NotificationType.ORDER_RECEIVED,
      timestamp: new Date().toISOString(),
      payload: { orderId: 'order-1', physicalFileId: 'file-1', message: '' },
    };

    await handler(notification);

    expect(fetchOrders).toHaveBeenCalledWith(orderService);
    expect(showSnackbar).toHaveBeenCalledWith(
      'Received Trial List for file Criminal - 12345-1 with priority class: High',
      '#b4e6ff',
      'Trial List received!',
      15000,
      expect.objectContaining({ label: 'View package #12345' })
    );

    const action = showSnackbar.mock.calls[0][4];
    action.onClick();
    expect(viewOrderDetails).toHaveBeenCalledWith(order);
  });

  it('shows fallback snackbar when order is not found', async () => {
    const fetchOrders = vi.fn().mockResolvedValue([]);
    const showSnackbar = vi.fn();
    const viewOrders = vi.fn();

    const handler = createOrderReceivedHandler({
      orderService: {} as any,
      ordersStore: { fetchOrders } as any,
      snackbarStore: { showSnackbar } as any,
      viewOrderDetails: vi.fn(),
      viewOrders,
    });

    const notification: NotificationDto<OrderReceivedNotificationPayload> = {
      type: NotificationType.ORDER_RECEIVED,
      timestamp: new Date().toISOString(),
      payload: {
        orderId: 'missing-order',
        physicalFileId: 'file-1',
        message: '',
      },
    };

    await handler(notification);

    expect(showSnackbar).toHaveBeenCalledWith(
      'Package received requiring signature/action',
      '#b4e6ff',
      'Notification received!',
      15000,
      expect.objectContaining({ label: 'View orders/applications' })
    );

    const action = showSnackbar.mock.calls[0][4];
    action.onClick();
    expect(viewOrders).toHaveBeenCalled();
  });
});
