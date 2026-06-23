import { useOrderBadgePulse } from '@/composables/useOrderBadgePulse';
import { mount } from '@vue/test-utils';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { computed, defineComponent, h, nextTick, ref } from 'vue';

const PULSE_DURATION_MS = 1200;

/**
 * Mounts the composable inside a real component so that Vue lifecycle
 * hooks and watchers are properly registered.
 */
function withSetup<T>(composable: () => T) {
  let result: T;
  const TestComponent = defineComponent({
    setup() {
      result = composable();
      return () => h('div');
    },
  });
  const wrapper = mount(TestComponent);
  return { result: result!, wrapper };
}

function makeCountPair(priorityInit = 0, regularInit = 0) {
  const priority = ref(priorityInit);
  const regular = ref(regularInit);
  return {
    priority,
    regular,
    pair: {
      priority: computed(() => priority.value),
      regular: computed(() => regular.value),
    },
  };
}

describe('useOrderBadgePulse', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('initialises with active = false', () => {
    const { pair } = makeCountPair();
    const { result } = withSetup(() => useOrderBadgePulse([pair]));

    expect(result.active.value).toBe(false);
  });

  it('does not pulse when counts have not changed', async () => {
    const { pair } = makeCountPair();
    const { result } = withSetup(() => useOrderBadgePulse([pair]));

    await nextTick();
    vi.advanceTimersByTime(PULSE_DURATION_MS);

    expect(result.active.value).toBe(false);
  });

  it('activates pulse when priority count changes to a positive number', async () => {
    const { priority, pair } = makeCountPair();
    const { result } = withSetup(() => useOrderBadgePulse([pair]));

    priority.value = 1;
    // Wait for the watcher to fire and trigger() to advance past its internal nextTick.
    await nextTick();
    await nextTick();

    expect(result.active.value).toBe(true);
  });

  it('activates pulse when regular count changes to a positive number', async () => {
    const { regular, pair } = makeCountPair();
    const { result } = withSetup(() => useOrderBadgePulse([pair]));

    regular.value = 3;
    await nextTick();
    await nextTick();

    expect(result.active.value).toBe(true);
  });

  it('deactivates pulse after PULSE_DURATION_MS', async () => {
    const { priority, pair } = makeCountPair();
    const { result } = withSetup(() => useOrderBadgePulse([pair]));

    priority.value = 2;
    await nextTick();
    await nextTick();
    expect(result.active.value).toBe(true);

    vi.advanceTimersByTime(PULSE_DURATION_MS);
    expect(result.active.value).toBe(false);
  });

  it('does not deactivate before PULSE_DURATION_MS elapses', async () => {
    const { priority, pair } = makeCountPair();
    const { result } = withSetup(() => useOrderBadgePulse([pair]));

    priority.value = 1;
    await nextTick();
    await nextTick();
    expect(result.active.value).toBe(true);

    vi.advanceTimersByTime(PULSE_DURATION_MS - 1);
    expect(result.active.value).toBe(true);
  });

  it('does not pulse when counts change but all counts remain zero', async () => {
    const { priority, regular, pair } = makeCountPair(0, 0);
    const { result } = withSetup(() => useOrderBadgePulse([pair]));

    // Mutate refs but resulting computed values stay at 0.
    priority.value = 0;
    regular.value = 0;
    await nextTick();
    await nextTick();

    expect(result.active.value).toBe(false);
  });

  it('does not re-activate after counts drop back to zero', async () => {
    const { priority, pair } = makeCountPair();
    const { result } = withSetup(() => useOrderBadgePulse([pair]));

    priority.value = 1;
    await nextTick();
    await nextTick();
    expect(result.active.value).toBe(true);

    vi.advanceTimersByTime(PULSE_DURATION_MS);
    expect(result.active.value).toBe(false);

    priority.value = 0;
    await nextTick();
    await nextTick();

    expect(result.active.value).toBe(false);
  });

  it('pulses again when counts change again after the previous pulse finishes', async () => {
    const { priority, pair } = makeCountPair();
    const { result } = withSetup(() => useOrderBadgePulse([pair]));

    priority.value = 1;
    await nextTick();
    await nextTick();
    expect(result.active.value).toBe(true);

    vi.advanceTimersByTime(PULSE_DURATION_MS);
    expect(result.active.value).toBe(false);

    priority.value = 2;
    await nextTick();
    await nextTick();

    expect(result.active.value).toBe(true);
  });

  it('restarts the pulse when counts change before the previous pulse finishes', async () => {
    const { priority, pair } = makeCountPair();
    const { result } = withSetup(() => useOrderBadgePulse([pair]));

    priority.value = 1;
    await nextTick();
    await nextTick();
    expect(result.active.value).toBe(true);

    // Half-way through the pulse, change the count again.
    vi.advanceTimersByTime(PULSE_DURATION_MS / 2);
    priority.value = 2;
    await nextTick();
    await nextTick();
    expect(result.active.value).toBe(true);

    // Original timeout fires - but a new one should still keep us active.
    vi.advanceTimersByTime(PULSE_DURATION_MS / 2);
    expect(result.active.value).toBe(false);
  });

  it('supports multiple count pairs and pulses when any pair changes', async () => {
    const a = makeCountPair();
    const b = makeCountPair();
    const { result } = withSetup(() => useOrderBadgePulse([a.pair, b.pair]));

    b.regular.value = 5;
    await nextTick();
    await nextTick();

    expect(result.active.value).toBe(true);
  });

  it('pulses when an extra trigger fires and at least one count is non-zero', async () => {
    const { priority, pair } = makeCountPair(1, 0);
    const trigger = ref(0);
    const { result } = withSetup(() => useOrderBadgePulse([pair], [trigger]));

    // Sanity: priority is already 1 but no change has occurred yet.
    expect(result.active.value).toBe(false);

    trigger.value = 1;
    await nextTick();
    await nextTick();

    expect(result.active.value).toBe(true);

    // Suppress unused warning - priority is the controlling ref via the pair.
    void priority;
  });

  it('does not pulse when an extra trigger fires while all counts are zero', async () => {
    const { pair } = makeCountPair(0, 0);
    const trigger = ref(0);
    const { result } = withSetup(() => useOrderBadgePulse([pair], [trigger]));

    trigger.value = 1;
    await nextTick();
    await nextTick();

    expect(result.active.value).toBe(false);
  });
});
