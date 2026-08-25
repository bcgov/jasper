import { createBroadcastChannel } from '@/utils/createBroadcastChannel';
import { afterEach, describe, expect, it, vi } from 'vitest';

type TestMessage = { type: string; value: number };

describe('createBroadcastChannel', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  describe('when BroadcastChannel is supported', () => {
    it('delivers a posted message to a subscriber', async () => {
      const channelName = `channel-${Math.random()}`;
      const producer = createBroadcastChannel<TestMessage>(channelName);
      const consumer = createBroadcastChannel<TestMessage>(channelName);

      const callback = vi.fn();
      const unsubscribe = consumer.subscribe(callback);

      const message: TestMessage = { type: 'ping', value: 42 };
      producer.post(message);

      await vi.waitFor(() => expect(callback).toHaveBeenCalledWith(message));

      unsubscribe();
    });

    it('stops delivering messages after unsubscribe', async () => {
      const channelName = `channel-${Math.random()}`;
      const producer = createBroadcastChannel<TestMessage>(channelName);
      const consumer = createBroadcastChannel<TestMessage>(channelName);

      const callback = vi.fn();
      const unsubscribe = consumer.subscribe(callback);
      unsubscribe();

      producer.post({ type: 'ping', value: 1 });

      await new Promise((resolve) => setTimeout(resolve, 0));
      expect(callback).not.toHaveBeenCalled();
    });

    it('does not deliver messages to subscribers on a different channel name', async () => {
      const producer = createBroadcastChannel<TestMessage>('channel-a');
      const consumer = createBroadcastChannel<TestMessage>('channel-b');

      const callback = vi.fn();
      const unsubscribe = consumer.subscribe(callback);

      producer.post({ type: 'ping', value: 1 });

      await new Promise((resolve) => setTimeout(resolve, 0));
      expect(callback).not.toHaveBeenCalled();

      unsubscribe();
    });
  });

  describe('when BroadcastChannel is not supported', () => {
    it('post is a no-op and does not throw', () => {
      vi.stubGlobal('BroadcastChannel', undefined);

      const channel = createBroadcastChannel<TestMessage>('unsupported');

      expect(() => channel.post({ type: 'ping', value: 1 })).not.toThrow();
    });

    it('subscribe returns a no-op unsubscribe function', () => {
      vi.stubGlobal('BroadcastChannel', undefined);

      const channel = createBroadcastChannel<TestMessage>('unsupported');
      const callback = vi.fn();

      const unsubscribe = channel.subscribe(callback);

      expect(typeof unsubscribe).toBe('function');
      expect(() => unsubscribe()).not.toThrow();
      expect(callback).not.toHaveBeenCalled();
    });
  });
});
