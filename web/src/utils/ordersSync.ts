import { createBroadcastChannel } from './createBroadcastChannel';

const ordersChannel = createBroadcastChannel<'order-submitted'>('orders-sync');

// Notify subscribers that an order has been submitted.
export const notifyOrderSubmitted = (): void =>
  ordersChannel.post('order-submitted');

export const subscribeToOrderSubmitted = (callback: () => void): (() => void) =>
  ordersChannel.subscribe(() => callback());
