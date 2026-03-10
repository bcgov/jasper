import { OrderService } from '@/services';
import { Order } from '@/types';
import { defineStore } from 'pinia';
import { ref, Ref, watch } from 'vue';

export const useOrdersStore = defineStore('orders', () => {
  const orders = ref<Order[]>([]);
  const isLoading = ref(false);
  const lastFetched = ref<Date | null>(null);
  const isInitialized = ref(false);
  const currentJudgeId = ref<number | null>(null);

  const setOrders = (newOrders: Order[]) => {
    orders.value = newOrders;
    lastFetched.value = new Date();
  };

  const fetchOrders = async (
    orderService: OrderService,
    judgeIdOverride?: number | null
  ) => {
    if (isLoading.value) {
      return;
    }

    isLoading.value = true;
    try {
      const judgeId = judgeIdOverride ?? currentJudgeId.value;
      const ordersData = await orderService.getOrders(judgeId ?? null);
      setOrders(ordersData ?? []);
    } catch {
      console.error('Failed to fetch orders');
    } finally {
      isLoading.value = false;
    }
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
        currentJudgeId.value = newJudgeId ?? null;
        await fetchOrders(orderService, currentJudgeId.value);
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
    currentJudgeId.value = null;
  };

  return {
    orders,
    isLoading,
    lastFetched,
    fetchOrders,
    setOrders,
    initialize,
    reset,
  };
});
