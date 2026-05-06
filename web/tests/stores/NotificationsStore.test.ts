import { type OrderService } from '@/services';
import { NotificationsService } from '@/signalr/notifications';
import { NotificationType } from '@/types/common';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const {
  fetchOrdersMock,
  showSnackbarMock,
  viewOrderDetailsMock,
  getCourtClassLabelMock,
} = vi.hoisted(() => ({
  fetchOrdersMock: vi.fn(),
  showSnackbarMock: vi.fn(),
  viewOrderDetailsMock: vi.fn(),
  getCourtClassLabelMock: vi.fn(),
}));

vi.mock('@/stores/OrdersStore', () => ({
  useOrdersStore: () => ({
    fetchOrders: fetchOrdersMock,
  }),
}));

vi.mock('@/stores/SnackbarStore', () => ({
  useSnackbarStore: () => ({
    showSnackbar: showSnackbarMock,
  }),
}));

vi.mock('@/utils/orderDetails', () => ({
  viewOrderDetails: viewOrderDetailsMock,
}));

vi.mock('@/utils/utils', () => ({
  getCourtClassLabel: getCourtClassLabelMock,
}));

import { useNotificationsStore } from '@/stores/NotificationsStore';

describe('NotificationsStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    getCourtClassLabelMock.mockReturnValue('Criminal');
  });

  it('registers one order handler and starts notifications once', async () => {
    const store = useNotificationsStore();
    let provider: (
      type: NotificationType
    ) => Iterable<(n: unknown) => unknown> = () => [];
    const notificationsService = new NotificationsService();
    const setHandlerProviderSpy = vi
      .spyOn(notificationsService, 'setHandlerProvider')
      .mockImplementation((p) => {
        provider = p;
      });
    const startSpy = vi
      .spyOn(notificationsService, 'start')
      .mockResolvedValue(undefined);

    await Promise.all([
      store.initialize(notificationsService, {} as OrderService),
      store.initialize(notificationsService, {} as OrderService),
    ]);

    expect(startSpy).toHaveBeenCalledTimes(1);
    expect(setHandlerProviderSpy).toHaveBeenCalledTimes(1);
    expect(Array.from(provider(NotificationType.ORDER_RECEIVED))).toHaveLength(
      1
    );
  });

  it('shows snackbar with action when received order is found', async () => {
    const order = {
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
      status: 'Pending',
    };
    fetchOrdersMock.mockResolvedValue([order]);

    const store = useNotificationsStore();
    let provider: (
      type: NotificationType
    ) => Iterable<(n: any) => Promise<void> | void> = () => [];
    const notificationsService = new NotificationsService();
    vi.spyOn(notificationsService, 'setHandlerProvider').mockImplementation(
      (p) => {
        provider = p;
      }
    );
    vi.spyOn(notificationsService, 'start').mockResolvedValue(undefined);
    const orderService = { getOrders: vi.fn() };

    await store.initialize(notificationsService, orderService as OrderService);

    const [handler] = Array.from(provider(NotificationType.ORDER_RECEIVED));
    await handler({
      type: NotificationType.ORDER_RECEIVED,
      timestamp: new Date().toISOString(),
      payload: { orderId: 'order-1' },
    });

    expect(fetchOrdersMock).toHaveBeenCalledWith(orderService);
    expect(showSnackbarMock).toHaveBeenCalledTimes(1);
    expect(showSnackbarMock).toHaveBeenCalledWith(
      'Received Trial List for file Criminal - 12345-1 with priority class: High',
      '#b4e6ff',
      'Notification received!',
      15000,
      expect.objectContaining({ label: 'View package #12345' })
    );

    const action = showSnackbarMock.mock.calls[0][4];
    action.onClick();
    expect(viewOrderDetailsMock).toHaveBeenCalledWith(order);
  });

  it('does not show snackbar when received order is missing', async () => {
    fetchOrdersMock.mockResolvedValue([]);

    const store = useNotificationsStore();
    let provider: (
      type: NotificationType
    ) => Iterable<(n: any) => Promise<void> | void> = () => [];
    const notificationsService = new NotificationsService();
    vi.spyOn(notificationsService, 'setHandlerProvider').mockImplementation(
      (p) => {
        provider = p;
      }
    );
    vi.spyOn(notificationsService, 'start').mockResolvedValue(undefined);

    await store.initialize(notificationsService, {} as OrderService);
    const [handler] = Array.from(provider(NotificationType.ORDER_RECEIVED));

    await handler({
      type: NotificationType.ORDER_RECEIVED,
      timestamp: new Date().toISOString(),
      payload: { orderId: 'missing-order' },
    });

    expect(showSnackbarMock).not.toHaveBeenCalled();
  });
});
