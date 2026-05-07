import type { OrderService } from '@/services';
import { useOrdersStore } from '@/stores/OrdersStore';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import type { Order } from '@/types';
import { NotificationType } from '@/types/common';
import { getCourtClassLabel } from '@/utils/utils';
import type { NotificationHandler } from '../notifications';
import { type OrderReceivedNotificationPayload } from '../payloads';
import { useRouter } from 'vue-router';

export const priorityText = (order: Order) =>
  order.priorityTypeDescription
    ? ` with priority class: ${order.priorityTypeDescription}`
    : ``;

export const buildOrderReceivedMessage = (order: Order) =>
  `Received ${order.courtListType} for file ${getCourtClassLabel(order.courtClass)} - ${order.courtFileNumber}${priorityText(order)}`;

export const createOrderReceivedHandler = ({
  orderService,
  ordersStore,
  snackbarStore,
  viewOrderDetails,
}: {
  orderService: OrderService;
  ordersStore: ReturnType<typeof useOrdersStore>;
  snackbarStore: ReturnType<typeof useSnackbarStore>;
  viewOrderDetails: (order: Order) => void;
}): NotificationHandler<OrderReceivedNotificationPayload> => {
  const router = useRouter();
  return async (notification) => {
    if (notification.type !== NotificationType.ORDER_RECEIVED) {
      return;
    }

    const newOrders = await ordersStore.fetchOrders(orderService);
    const order = newOrders?.find(
      (o) => o.id === notification.payload?.orderId
    );

    if (!order) {
      // display fallback message if new order matching notification can not be retrieved.
      snackbarStore.showSnackbar(
        'Package received requiring signature/action',
        '#b4e6ff',
        'Notification received!',
        15000,
        {
          label: `View orders/applications`,
          onClick: () => router.push('/orders'),
        }
      );
      return;
    }

    snackbarStore.showSnackbar(
      buildOrderReceivedMessage(order),
      '#b4e6ff',
      `${order.courtListType} received!`,
      15000,
      {
        label: `View package #${order.packageId}`,
        onClick: () => viewOrderDetails(order),
      }
    );
  };
};
