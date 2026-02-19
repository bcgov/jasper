import { OrderService } from '@/services';
import { Order } from '@/types';
import { defineStore } from 'pinia';
import { ref, Ref, watch } from 'vue';

export const useOrdersStore = defineStore('orders', () => {
  const orders = ref<Order[]>([]);
  const isLoading = ref(false);
  const lastFetched = ref<Date | null>(null);
  const isInitialized = ref(false);

  const setOrders = (newOrders: Order[]) => {
    orders.value = newOrders;
    lastFetched.value = new Date();
  };

  const initialize = (
    orderService: OrderService,
    judgeIdSource: Ref<number | null | undefined>
  ) => {
    if (isInitialized.value) {
      return;
    }

    // Watch the reactive judgeId source and auto-fetch
    watch(
      judgeIdSource,
      async (newJudgeId) => {
        if (isLoading.value) {
          return;
        }

        isLoading.value = true;
        try {
          const ordersData = await orderService.getOrders(newJudgeId ?? null);
          setOrders(ordersData ?? []);
        } catch {
          console.error('Failed to fetch orders');
        } finally {
          isLoading.value = false;
        }
      },
      { immediate: true }
    );

    isInitialized.value = true;
  };

  const reset = () => {
    orders.value = [];
    lastFetched.value = null;
    isLoading.value = false;
    isInitialized.value = false;
  };

  return {
    orders,
    isLoading,
    lastFetched,
    setOrders,
    initialize,
    reset,
  };
});
