export type BroadcastChannelApi<TMessage> = {
  post: (message: TMessage) => void;
  subscribe: (callback: (message: TMessage) => void) => () => void;
};

// Create a broadcast channel for inter-tab communication.
export const createBroadcastChannel = <TMessage>(
  channelName: string
): BroadcastChannelApi<TMessage> => {
  const isSupported = typeof BroadcastChannel !== 'undefined';

  const post = (message: TMessage): void => {
    if (!isSupported) {
      return;
    }

    const channel = new BroadcastChannel(channelName);
    channel.postMessage(message);
    channel.close();
  };

  const subscribe = (callback: (message: TMessage) => void): (() => void) => {
    if (!isSupported) {
      return () => {};
    }

    const channel = new BroadcastChannel(channelName);
    const listener = (event: MessageEvent<TMessage>) => callback(event.data);

    channel.addEventListener('message', listener);

    return () => {
      channel.removeEventListener('message', listener);
      channel.close();
    };
  };

  return { post, subscribe };
};
