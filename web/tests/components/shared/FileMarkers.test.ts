import { FileMarkerEnum } from '@/types/common';
import { faker } from '@faker-js/faker';
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

  it('renders child divs when there are multiple notes', async () => {
    const fakeNote1 = faker.lorem.words(5);
    const fakeNote2 = faker.lorem.words(5);
    const fakeNote3 = faker.lorem.words(5);

    const wrapper = mount(FileMarkers, {
      props: {
        markers: [
          {
            marker: FileMarkerEnum.ADJ,
            value: '',
            notes: [fakeNote1, fakeNote2, fakeNote3],
          },
        ],
      },
    });

    await nextTick();

    const tooltip = wrapper.find('v-tooltip div');
    const notes = tooltip.findAll('div');

    expect(notes.length).toBe(3);
    expect(notes[0].text()).toEqual(fakeNote1);
    expect(notes[1].text()).toEqual(fakeNote2);
    expect(notes[2].text()).toEqual(fakeNote3);
  });
});
