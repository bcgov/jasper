import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const post = vi.fn();
const subscribe = vi.fn();
const createBroadcastChannel = vi.fn(() => ({ post, subscribe }));

vi.mock('@/utils/createBroadcastChannel', () => ({
  createBroadcastChannel,
}));

describe('ordersSync', () => {
  beforeEach(() => {
    vi.resetModules();
    post.mockClear();
    subscribe.mockClear();
    createBroadcastChannel.mockClear();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('creates the broadcast channel with the "orders-sync" name', async () => {
    await import('@/utils/ordersSync');

    expect(createBroadcastChannel).toHaveBeenCalledWith('orders-sync');
  });

  it('notifyOrderSubmitted posts the "order-submitted" message', async () => {
    const { notifyOrderSubmitted } = await import('@/utils/ordersSync');

    notifyOrderSubmitted();

    expect(post).toHaveBeenCalledTimes(1);
    expect(post).toHaveBeenCalledWith('order-submitted');
  });

  it('subscribeToOrderSubmitted registers a subscriber', async () => {
    const { subscribeToOrderSubmitted } = await import('@/utils/ordersSync');
    const callback = vi.fn();

    subscribeToOrderSubmitted(callback);

    expect(subscribe).toHaveBeenCalledTimes(1);
    expect(subscribe).toHaveBeenCalledWith(expect.any(Function));
  });

  it('invokes the callback when the channel emits a message', async () => {
    const { subscribeToOrderSubmitted } = await import('@/utils/ordersSync');
    const callback = vi.fn();

    subscribeToOrderSubmitted(callback);

    // Emulate the channel delivering a message to the registered listener.
    const listener = subscribe.mock.calls[0][0] as (message: unknown) => void;
    listener('order-submitted');

    expect(callback).toHaveBeenCalledTimes(1);
    expect(callback).toHaveBeenCalledWith();
  });

  it('returns the unsubscribe function from the channel', async () => {
    const unsubscribe = vi.fn();
    subscribe.mockReturnValueOnce(unsubscribe);

    const { subscribeToOrderSubmitted } = await import('@/utils/ordersSync');
    const result = subscribeToOrderSubmitted(vi.fn());

    expect(result).toBe(unsubscribe);
  });
});
