import { nextTick, ref, watch, type ComputedRef, type WatchSource } from 'vue';

const PULSE_DURATION_MS = 1200;

export interface CountPair {
  priority: ComputedRef<number>;
  regular: ComputedRef<number>;
}

/**
 * Triggers animation on an order badge when any of the priority or regular counts change.
 *
 * @param countPairs - Pairs of priority/regular count refs to watch.
 * @param extraTriggers - Additional reactive sources that should also re-evaluate the pulse.
 * @returns An object with `active`, a ref that is `true` while the pulse animation should play.
 */
export function useOrderBadgePulse(
  countPairs: ReadonlyArray<CountPair>,
  extraTriggers: ReadonlyArray<WatchSource> = []
) {
  const active = ref(false);

  const trigger = async () => {
    const shouldPulse = countPairs.some(
      ({ priority, regular }) => priority.value > 0 || regular.value > 0
    );
    if (!shouldPulse) {
      return;
    }

    active.value = false;
    await nextTick();
    active.value = true;
    globalThis.setTimeout(() => {
      active.value = false;
    }, PULSE_DURATION_MS);
  };

  const sources: WatchSource[] = [
    ...countPairs.flatMap(({ priority, regular }) => [priority, regular]),
    ...extraTriggers,
  ];

  watch(sources, () => {
    void trigger();
  });

  return { active };
}
