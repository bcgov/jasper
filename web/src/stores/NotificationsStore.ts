import { OrderService } from '@/services';
import { createOrderReceivedHandler } from '@/signalr/handlers/orderReceivedHandler';
import {
  type NotificationHandler,
  type NotificationsService,
} from '@/signalr/notifications';
import { NotificationType } from '@/types/common';
import { viewOrderDetails } from '@/utils/orderDetails';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useOrdersStore } from './OrdersStore';
import { useSnackbarStore } from './SnackbarStore';

export const useNotificationsStore = defineStore('notifications', () => {
  const handlers = ref(
    new Map<NotificationType, Set<NotificationHandler<unknown>>>()
  );
  const isStarted = ref(false);
  const hasOrderHandler = ref(false);
  const startTask = ref<Promise<void> | null>(null);
  const router = useRouter();
  const viewOrders = () => router.push('/orders');

  const registerHandler = <TPayload>(
    notificationType: NotificationType,
    handler: NotificationHandler<TPayload>
  ) => {
    const widenedHandler = handler as NotificationHandler<unknown>;
    const existingHandlers = handlers.value.get(notificationType);
    if (existingHandlers) {
      existingHandlers.add(widenedHandler);
    } else {
      handlers.value.set(notificationType, new Set([widenedHandler]));
    }

    return () => {
      const typeHandlers = handlers.value.get(notificationType);
      if (!typeHandlers) {
        return false;
      }

      const deleted = typeHandlers.delete(widenedHandler);
      if (typeHandlers.size === 0) {
        handlers.value.delete(notificationType);
      }

      return deleted;
    };
  };

  const initialize = async (
    notificationsService: NotificationsService,
    orderService: OrderService
  ) => {
    if (!hasOrderHandler.value) {
      const ordersStore = useOrdersStore();
      const snackbarStore = useSnackbarStore();
      registerHandler(
        NotificationType.ORDER_RECEIVED,
        createOrderReceivedHandler({
          orderService,
          ordersStore,
          snackbarStore,
          viewOrderDetails,
          viewOrders,
        })
      );
      hasOrderHandler.value = true;
    }

    if (isStarted.value) {
      return;
    }

    startTask.value ??= (async () => {
      notificationsService.setHandlerProvider(
        (type) => handlers.value.get(type)?.values() ?? []
      );
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
