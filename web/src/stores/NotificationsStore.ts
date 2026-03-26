import { OrderService } from '@/services';
import {
  type NotificationHandler,
  NotificationType,
  type NotificationsService,
} from '@/signalr/notifications';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useOrdersStore } from './OrdersStore';

export const useNotificationsStore = defineStore('notifications', () => {
  const handlers = ref(new Set<NotificationHandler>());
  const isStarted = ref(false);
  const hasOrderHandler = ref(false);

  const registerHandler = (handler: NotificationHandler) => {
    handlers.value.add(handler);
    return () => handlers.value.delete(handler);
  };

  const initialize = async (
    notificationsService: NotificationsService,
    orderService: OrderService
  ) => {
    if (!hasOrderHandler.value) {
      const ordersStore = useOrdersStore();
      registerHandler((notification) => {
        if (notification.type === NotificationType.ORDER_RECEIVED) {
          ordersStore.fetchOrders(orderService);
        }
      });
      hasOrderHandler.value = true;
    }

    if (!isStarted.value) {
      notificationsService.setHandlerProvider(() => handlers.value.values());
      await notificationsService.start();
      isStarted.value = true;
    }
  };

  return {
    registerHandler,
    initialize,
  };
});
