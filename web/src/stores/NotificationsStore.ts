import { OrderService } from '@/services';
import {
  NotificationDto,
  type NotificationHandler,
  type NotificationsService,
} from '@/signalr/notifications';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useOrdersStore } from './OrdersStore';
import { NotificationType } from '@/types/common';
import { useSnackbarStore } from './SnackbarStore';
import { OrderReceivedNotificationPayload } from '@/signalr/payloads';
import { getCourtClassLabel } from '@/utils/utils';
import { viewOrderDetails } from '@/utils/orderDetails';

export const useNotificationsStore = defineStore('notifications', () => {
  const handlers = ref(new Set<NotificationHandler<unknown>>());
  const isStarted = ref(false);
  const hasOrderHandler = ref(false);
  const startTask = ref<Promise<void> | null>(null);
  const snackBarStore = useSnackbarStore();

  const registerHandler = <TPayload>(
    handler: NotificationHandler<TPayload>
  ) => {
    const widenedHandler = handler as NotificationHandler<unknown>;
    handlers.value.add(widenedHandler);
    return () => handlers.value.delete(widenedHandler);
  };

  const initialize = async (
    notificationsService: NotificationsService,
    orderService: OrderService
  ) => {
    if (!hasOrderHandler.value) {
      const ordersStore = useOrdersStore();
      registerHandler(
        async (
          notification: NotificationDto<OrderReceivedNotificationPayload>
        ) => {
          if (notification.type === NotificationType.ORDER_RECEIVED) {
            const newOrders = await ordersStore.fetchOrders(orderService);
            const notificationOrderId = notification.payload?.orderId;
            const notificationOrder = newOrders?.find(
              (o) => o.id === notificationOrderId
            );
            if (notificationOrder) {
              snackBarStore.showSnackbar(
                `Received order for file ${getCourtClassLabel(notificationOrder.courtClass)} - ${notificationOrder.courtFileNumber}.`,
                '#b4e6ff',
                '🔄 Heads-up!',
                15000,
                {
                  label: `Package ${notificationOrder.packageId}`,
                  onClick: () => viewOrderDetails(notificationOrder),
                }
              );
            }
          }
        }
      );
      hasOrderHandler.value = true;
    }

    if (isStarted.value) {
      return;
    }

    startTask.value ??= (async () => {
      notificationsService.setHandlerProvider(() => handlers.value.values());
      await notificationsService.start();
      isStarted.value = true;
    })();

    try {
      await startTask.value;
    } finally {
      if (!isStarted.value) {
        startTask.value = null;
      }
    }
  };

  return {
    registerHandler,
    initialize,
  };
});
