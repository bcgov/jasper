import type { Order } from '@/types';
import {
  OrderCourtLisTypeEnum,
  OrderPriorityEnum,
  OrderReviewStatus,
} from '@/types/common';
import { computed, type ComputedRef } from 'vue';

const PRIORITY_ORDER_TYPES = new Set<string>(Object.values(OrderPriorityEnum));

export const isPendingOrder = (order: Order): boolean =>
  order.status === OrderReviewStatus.Pending;

export const isPriorityOrder = (order: Order): boolean =>
  PRIORITY_ORDER_TYPES.has(order.priorityType);

export const isDeskOrder = (order: Order): boolean =>
  order.courtListType === OrderCourtLisTypeEnum.PSM ||
  order.courtListType === OrderCourtLisTypeEnum.PFM;

export interface PendingOrderCounts {
  priority: ComputedRef<number>;
  regular: ComputedRef<number>;
}

/**
 * Reactive counts of pending priority/regular orders matching an optional predicate.
 *
 * @param ordersAccessor Accessor returning the orders list to count over.
 * @param matches        Optional filter applied in addition to the pending-status check.
 */
export function useOrderCounts(
  ordersAccessor: () => readonly Order[],
  matches: (order: Order) => boolean = () => true
): PendingOrderCounts {
  const pending = computed(() =>
    ordersAccessor().filter((o) => isPendingOrder(o) && matches(o))
  );

  const priority = computed(() => pending.value.filter(isPriorityOrder).length);

  const regular = computed(() => pending.value.length - priority.value);

  return { priority, regular };
}
