import { config } from '@vue/test-utils';

config.global.config = config.global.config || {};
config.global.config.warnHandler = (msg, vm, trace) => {
  if (
    msg.includes('Failed to resolve component') ||
    msg.includes('Failed to resolve directive')
  ) {
    return;
  }
};
