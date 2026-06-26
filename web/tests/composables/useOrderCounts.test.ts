import {
  isDeskOrder,
  isPendingOrder,
  isPriorityOrder,
  useOrderCounts,
} from '@/composables/useOrderCounts';
import type { Order } from '@/types';
import {
  OrderCourtLisTypeEnum,
  OrderPriorityEnum,
  OrderReviewStatus,
} from '@/types/common';
import { describe, expect, it } from 'vitest';
import { ref } from 'vue';

const makeOrder = (overrides: Partial<Order> = {}): Order =>
  ({
    status: OrderReviewStatus.Pending,
    priorityType: 'NONPRIORITY',
    courtListTypeDescription: 'Daily List',
    ...overrides,
  }) as Order;

describe('useOrderCounts', () => {
  describe('helpers', () => {
    describe('isPendingOrder', () => {
      it('returns true when status is Pending', () => {
        expect(
          isPendingOrder(makeOrder({ status: OrderReviewStatus.Pending }))
        ).toBe(true);
      });

      it('returns false for non-pending statuses', () => {
        expect(
          isPendingOrder(makeOrder({ status: OrderReviewStatus.Approved }))
        ).toBe(false);
        expect(
          isPendingOrder(
            makeOrder({ status: OrderReviewStatus.AwaitingDocumentation })
          )
        ).toBe(false);
      });
    });

    describe('isPriorityOrder', () => {
      it.each([
        OrderPriorityEnum.ProtectionOrders,
        OrderPriorityEnum.CourtDirected,
        OrderPriorityEnum.Other,
      ])('returns true for priority type %s', (priorityType) => {
        expect(isPriorityOrder(makeOrder({ priorityType }))).toBe(true);
      });

      it('returns false for non-priority types', () => {
        expect(
          isPriorityOrder(makeOrder({ priorityType: 'NONPRIORITY' }))
        ).toBe(false);
        expect(isPriorityOrder(makeOrder({ priorityType: '' }))).toBe(false);
        expect(isPriorityOrder(makeOrder({ priorityType: 'UNKNOWN' }))).toBe(
          false
        );
      });
    });

    describe('isDeskOrder', () => {
      it('returns true when courtListType is PFM or PSM', () => {
        expect(
          isDeskOrder(makeOrder({ courtListType: OrderCourtLisTypeEnum.PFM }))
        ).toBe(true);
        expect(
          isDeskOrder(makeOrder({ courtListType: OrderCourtLisTypeEnum.PSM }))
        ).toBe(true);
      });

      it('returns false for any other courtListType', () => {
        expect(
          isDeskOrder(makeOrder({ courtListType: OrderCourtLisTypeEnum.PCS }))
        ).toBe(false);
        expect(
          isDeskOrder(makeOrder({ courtListType: OrderCourtLisTypeEnum.PFA }))
        ).toBe(false);
      });
    });
  });

  describe('useOrderCounts', () => {
    it('returns zero counts for an empty list', () => {
      const { priority, regular } = useOrderCounts(() => []);

      expect(priority.value).toBe(0);
      expect(regular.value).toBe(0);
    });

    it('ignores non-pending orders', () => {
      const orders: Order[] = [
        makeOrder({
          status: OrderReviewStatus.Approved,
          priorityType: OrderPriorityEnum.ProtectionOrders,
        }),
        makeOrder({
          status: OrderReviewStatus.AwaitingDocumentation,
          priorityType: 'NONPRIORITY',
        }),
      ];

      const { priority, regular } = useOrderCounts(() => orders);

      expect(priority.value).toBe(0);
      expect(regular.value).toBe(0);
    });

    it('counts pending priority orders into priority', () => {
      const orders: Order[] = [
        makeOrder({ priorityType: OrderPriorityEnum.ProtectionOrders }),
        makeOrder({ priorityType: OrderPriorityEnum.CourtDirected }),
        makeOrder({ priorityType: OrderPriorityEnum.Other }),
      ];

      const { priority, regular } = useOrderCounts(() => orders);

      expect(priority.value).toBe(3);
      expect(regular.value).toBe(0);
    });

    it('counts pending non-priority orders into regular', () => {
      const orders: Order[] = [
        makeOrder({ priorityType: 'NONPRIORITY' }),
        makeOrder({ priorityType: 'NONPRIORITY' }),
      ];

      const { priority, regular } = useOrderCounts(() => orders);

      expect(priority.value).toBe(0);
      expect(regular.value).toBe(2);
    });

    it('splits a mixed list into priority and regular counts', () => {
      const orders: Order[] = [
        makeOrder({ priorityType: OrderPriorityEnum.ProtectionOrders }),
        makeOrder({ priorityType: OrderPriorityEnum.CourtDirected }),
        makeOrder({ priorityType: 'NONPRIORITY' }),
        makeOrder({ priorityType: 'NONPRIORITY' }),
        makeOrder({ priorityType: 'NONPRIORITY' }),
        // Non-pending should be excluded.
        makeOrder({
          status: OrderReviewStatus.Approved,
          priorityType: OrderPriorityEnum.ProtectionOrders,
        }),
      ];

      const { priority, regular } = useOrderCounts(() => orders);

      expect(priority.value).toBe(2);
      expect(regular.value).toBe(3);
    });

    it('applies the matches predicate in addition to pending status', () => {
      const orders: Order[] = [
        makeOrder({
          priorityType: OrderPriorityEnum.ProtectionOrders,
          courtListType: OrderCourtLisTypeEnum.PSM,
        }),
        makeOrder({
          priorityType: 'NONPRIORITY',
          courtListType: OrderCourtLisTypeEnum.PSM,
        }),
        makeOrder({
          priorityType: OrderPriorityEnum.CourtDirected,
          courtListType: OrderCourtLisTypeEnum.PCS,
        }),
        makeOrder({
          priorityType: 'NONPRIORITY',
          courtListType: OrderCourtLisTypeEnum.PFA,
        }),
      ];

      const desk = useOrderCounts(() => orders, isDeskOrder);
      const nonDesk = useOrderCounts(
        () => orders,
        (o) => !isDeskOrder(o)
      );

      expect(desk.priority.value).toBe(1);
      expect(desk.regular.value).toBe(1);
      expect(nonDesk.priority.value).toBe(1);
      expect(nonDesk.regular.value).toBe(1);
    });

    it('treats a predicate that excludes everything as zero counts', () => {
      const orders: Order[] = [
        makeOrder({ priorityType: OrderPriorityEnum.ProtectionOrders }),
        makeOrder({ priorityType: 'NONPRIORITY' }),
      ];

      const { priority, regular } = useOrderCounts(
        () => orders,
        () => false
      );

      expect(priority.value).toBe(0);
      expect(regular.value).toBe(0);
    });

    it('is reactive: counts update when the underlying orders ref changes', () => {
      const orders = ref<Order[]>([]);
      const { priority, regular } = useOrderCounts(() => orders.value);

      expect(priority.value).toBe(0);
      expect(regular.value).toBe(0);

      orders.value = [
        makeOrder({ priorityType: OrderPriorityEnum.ProtectionOrders }),
        makeOrder({ priorityType: 'NONPRIORITY' }),
        makeOrder({ priorityType: 'NONPRIORITY' }),
      ];

      expect(priority.value).toBe(1);
      expect(regular.value).toBe(2);

      // Mark the priority order as approved -- it should drop out of both counts.
      orders.value = [
        makeOrder({
          status: OrderReviewStatus.Approved,
          priorityType: OrderPriorityEnum.ProtectionOrders,
        }),
        makeOrder({ priorityType: 'NONPRIORITY' }),
        makeOrder({ priorityType: 'NONPRIORITY' }),
      ];

      expect(priority.value).toBe(0);
      expect(regular.value).toBe(2);
    });
  });
});
