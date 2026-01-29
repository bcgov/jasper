import { useOrdersStore } from '@/stores/OrdersStore';
import { Order } from '@/types';
import { OrderStatusEnum } from '@/types/common';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// Mock OrderService
vi.mock('@/services', () => ({
  OrderService: vi.fn(),
}));

describe('OrdersStore', () => {
  let store: ReturnType<typeof useOrdersStore>;
  let mockOrderService: any;

  const mockOrders: Order[] = [
    {
      id: '1',
      packageNumber: 12345,
      receivedDate: '2026-01-15',
      processedDate: '2026-01-20',
      courtClass: 'CC',
      courtFileNumber: 'CF-2026-001',
      styleOfCause: 'R v Smith',
      physicalFileId: 'file-001',
      status: OrderStatusEnum.Pending,
    },
    {
      id: '2',
      packageNumber: 67890,
      receivedDate: '2026-01-14',
      processedDate: '2026-01-21',
      courtClass: 'CV',
      courtFileNumber: 'CV-2026-001',
      styleOfCause: 'Jones v Brown',
      physicalFileId: 'file-002',
      status: OrderStatusEnum.Approved,
    },
  ];

  beforeEach(() => {
    setActivePinia(createPinia());
    store = useOrdersStore();

    mockOrderService = {
      getOrders: vi.fn(),
    };
  });

  it('initializes with default values', () => {
    expect(store.orders).toEqual([]);
    expect(store.isLoading).toBe(false);
    expect(store.lastFetched).toBeNull();
  });

  describe('setOrders', () => {
    it('should set orders and update lastFetched', () => {
      const beforeTime = new Date();

      store.setOrders(mockOrders);

      expect(store.orders).toEqual(mockOrders);
      expect(store.lastFetched).toBeInstanceOf(Date);
      expect(store.lastFetched!.getTime()).toBeGreaterThanOrEqual(
        beforeTime.getTime()
      );
    });

    it('should replace existing orders when called multiple times', () => {
      store.setOrders(mockOrders);
      expect(store.orders).toHaveLength(2);

      const newOrders = [mockOrders[0]];
      store.setOrders(newOrders);

      expect(store.orders).toHaveLength(1);
      expect(store.orders).toEqual(newOrders);
    });

    it('should handle empty array', () => {
      store.setOrders(mockOrders);
      store.setOrders([]);

      expect(store.orders).toEqual([]);
      expect(store.lastFetched).toBeInstanceOf(Date);
    });
  });

  describe('fetchOrders', () => {
    it('should fetch orders successfully', async () => {
      mockOrderService.getOrders.mockResolvedValueOnce(mockOrders);

      await store.fetchOrders(mockOrderService);

      expect(mockOrderService.getOrders).toHaveBeenCalledTimes(1);
      expect(store.orders).toEqual(mockOrders);
      expect(store.isLoading).toBe(false);
      expect(store.lastFetched).toBeInstanceOf(Date);
    });

    it('should set isLoading to true during fetch', async () => {
      let isLoadingDuringFetch = false;

      mockOrderService.getOrders.mockImplementation(async () => {
        isLoadingDuringFetch = store.isLoading;
        return mockOrders;
      });

      await store.fetchOrders(mockOrderService);

      expect(isLoadingDuringFetch).toBe(true);
      expect(store.isLoading).toBe(false);
    });

    it('should set isLoading to false after fetch completes', async () => {
      mockOrderService.getOrders.mockResolvedValueOnce(mockOrders);

      await store.fetchOrders(mockOrderService);

      expect(store.isLoading).toBe(false);
    });

    it('should set isLoading to false if fetch fails', async () => {
      mockOrderService.getOrders.mockRejectedValueOnce(new Error('API Error'));

      expect(store.isLoading).toBe(false);
    });

    it('should handle null response from service', async () => {
      mockOrderService.getOrders.mockResolvedValueOnce(null);

      await store.fetchOrders(mockOrderService);

      expect(store.orders).toEqual([]);
      expect(store.isLoading).toBe(false);
    });

    it('should handle undefined response from service', async () => {
      mockOrderService.getOrders.mockResolvedValueOnce(undefined);

      await store.fetchOrders(mockOrderService);

      expect(store.orders).toEqual([]);
      expect(store.isLoading).toBe(false);
    });

    it('should not fetch if already loading', async () => {
      mockOrderService.getOrders.mockImplementation(
        () =>
          new Promise((resolve) => setTimeout(() => resolve(mockOrders), 100))
      );

      // Start first fetch
      const firstFetch = store.fetchOrders(mockOrderService);

      // Try to start second fetch while first is in progress
      await store.fetchOrders(mockOrderService);

      // Complete first fetch
      await firstFetch;

      // Should only be called once
      expect(mockOrderService.getOrders).toHaveBeenCalledTimes(1);
    });

    it('should handle empty array response', async () => {
      mockOrderService.getOrders.mockResolvedValueOnce([]);

      await store.fetchOrders(mockOrderService);

      expect(store.orders).toEqual([]);
      expect(store.isLoading).toBe(false);
    });
  });

  describe('reset', () => {
    it('should reset all state to initial values', () => {
      // Set some data first
      store.setOrders(mockOrders);
      store.isLoading = true;

      store.reset();

      expect(store.orders).toEqual([]);
      expect(store.lastFetched).toBeNull();
      expect(store.isLoading).toBe(false);
    });

    it('should reset state even when loading', () => {
      store.isLoading = true;
      store.setOrders(mockOrders);

      store.reset();

      expect(store.orders).toEqual([]);
      expect(store.lastFetched).toBeNull();
      expect(store.isLoading).toBe(false);
    });
  });
});
