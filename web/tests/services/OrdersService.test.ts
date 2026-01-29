import { IHttpService } from '@/services/HttpService';
import { OrderService } from '@/services/OrderService';
import { Order } from '@/types';
import { OrderStatusEnum } from '@/types/common';
import { beforeEach, describe, expect, it, Mock, vi } from 'vitest';

const mockHttpService = {
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn(),
} as unknown as IHttpService;

class TestableOrderService extends OrderService {
  constructor() {
    super(mockHttpService);
  }
}

describe('OrderService', () => {
  let service: OrderService;

  beforeEach(() => {
    vi.clearAllMocks();
    service = new TestableOrderService();
  });

  const mockOrders: Order[] = [
    {
      id: '1',
      packageId: 12345,
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
      packageId: 67890,
      receivedDate: '2026-01-14',
      processedDate: '2026-01-21',
      courtClass: 'CV',
      courtFileNumber: 'CV-2026-001',
      styleOfCause: 'Jones v Brown',
      physicalFileId: 'file-002',
      status: OrderStatusEnum.Approved,
    },
  ];

  describe('getOrders', () => {
    it('should fetch orders with judgeId when provided', async () => {
      const judgeId = 123;
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockOrders);

      const result = await service.getOrders(judgeId);

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/orders?judgeId=123'
      );
      expect(result).toEqual(mockOrders);
    });

    it('should fetch orders with empty judgeId when null is provided', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockOrders);

      const result = await service.getOrders(null);

      expect(mockHttpService.get).toHaveBeenCalledWith('api/orders?judgeId=');
      expect(result).toEqual(mockOrders);
    });

    it('should fetch orders with empty judgeId when no parameter is provided', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockOrders);

      const result = await service.getOrders();

      expect(mockHttpService.get).toHaveBeenCalledWith('api/orders?judgeId=');
      expect(result).toEqual(mockOrders);
    });

    it('should handle empty orders array', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce([]);

      const result = await service.getOrders(123);

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/orders?judgeId=123'
      );
      expect(result).toEqual([]);
    });

    it('should handle API errors', async () => {
      const error = new Error('API Error');
      (mockHttpService.get as Mock).mockRejectedValueOnce(error);

      await expect(service.getOrders(123)).rejects.toThrow('API Error');
      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/orders?judgeId=123'
      );
    });

    it('should fetch orders with judgeId 0', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockOrders);

      const result = await service.getOrders(0);

      expect(mockHttpService.get).toHaveBeenCalledWith('api/orders?judgeId=0');
      expect(result).toEqual(mockOrders);
    });
  });
});
