import { OrderService } from '@/services';
import { useOrdersStore } from '@/stores';
import { NotificationType } from '@/types/common';
import { NotificationHandler } from '../notifications';
import { OrderSubmittedNotificationPayload } from '../payloads';

export const createOrderSubmittedHandler = ({
  orderService,
  ordersStore,
}: {
  orderService: OrderService;
  ordersStore: ReturnType<typeof useOrdersStore>;
}): NotificationHandler<OrderSubmittedNotificationPayload> => {
  return async (notification) => {
    if (notification.type !== NotificationType.ORDER_SUBMITTED) {
      return;
    }
    await ordersStore.fetchOrders(orderService);
  };
};
