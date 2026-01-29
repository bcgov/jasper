import { OrderService } from '@/services';
import { Order } from '@/types';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useOrdersStore = defineStore('orders', () => {
  const orders = ref<Order[]>([]);
  const isLoading = ref(false);
  const lastFetched = ref<Date | null>(null);

  const setOrders = (newOrders: Order[]) => {
    orders.value = newOrders;
    lastFetched.value = new Date();
  };

  const fetchOrders = async (orderService: OrderService) => {
    if (isLoading.value) return;

    isLoading.value = true;
    try {
      const ordersData = await orderService.getOrders();
      setOrders(ordersData ?? []);
    } catch {
      console.error('Failed to fetch orders');
    } finally {
      isLoading.value = false;
    }
  };

  const reset = () => {
    orders.value = [];
    lastFetched.value = null;
    isLoading.value = false;
  };

  return {
    orders,
    isLoading,
    lastFetched,
    setOrders,
    fetchOrders,
    reset,
  };
});
