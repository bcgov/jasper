import { mount, VueWrapper } from '@vue/test-utils'
import { nextTick } from 'vue'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import CopyToClipboard from 'CMP/shared/CopyToClipboard.vue'

describe('CopyToClipboard.vue', () => {
  let wrapper: VueWrapper<any>;
  const writeText = vi.fn();

  beforeEach(() => {
    Object.defineProperty(global.navigator, 'clipboard', {
      value: { writeText },
      configurable: true
    })
    wrapper = mount(CopyToClipboard, {
      props: { text: 'Hello, clipboard!' }
    })
  })

  it('does not show tooltip by default', () => {
    expect(wrapper.vm.showTooltip).toBe(false);
  })

  it('hides tooltip after delay', async () => {
    vi.useFakeTimers();
    wrapper.vm.showTooltip = true;
    await nextTick();
    expect(wrapper.vm.showTooltip).toBe(true);
    vi.advanceTimersByTime(1000);
    await nextTick();
    expect(wrapper.vm.showTooltip).toBe(false);
    vi.useRealTimers();
  })
})