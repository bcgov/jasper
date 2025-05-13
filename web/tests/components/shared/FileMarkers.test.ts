import { FileMarkerEnum } from '@/types/common';
import { mount } from '@vue/test-utils';
import FileMarkers from 'CMP/shared/FileMarkers.vue';
import { describe, expect, it } from 'vitest';
import { nextTick } from 'vue';

describe('FileMarkers.vue', () => {
  const props = {
    classOverride: 'pt-1',
    markers: [
      { marker: FileMarkerEnum.W, value: 'Y' },
      { marker: FileMarkerEnum.IC, value: 'N' },
      { marker: FileMarkerEnum.DO, value: 'Y' },
      { marker: FileMarkerEnum.INT, value: 'N' },
    ],
  };

  it('renders markers', async () => {
    const wrapper = mount(FileMarkers, { props });

    await nextTick();

    const chips = wrapper.findAll('v-chip');
    const selectedChips = wrapper.findAll('v-chip.selected');

    expect(chips.length).toBe(4); // W, IC, DO, INT
    expect(selectedChips.length).toBe(2); // W, INT
    expect(chips[0].classes()).toContain('pt-1');
  });

  it('renders correct marker descriptions in tooltips', async () => {
    const wrapper = mount(FileMarkers, { props });

    await nextTick();

    const tooltips = wrapper.findAll('v-tooltip');
    const descriptions = tooltips.map((tooltip) => tooltip.text());

    expect(tooltips.length).toBe(4);
    expect(descriptions).toContain('Warrant Issued');
    expect(descriptions).toContain('In Custody');
    expect(descriptions).toContain('Detention Order');
    expect(descriptions).toContain('Interpreter Required');
  });
});
