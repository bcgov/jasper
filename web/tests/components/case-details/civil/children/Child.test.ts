import Child from '@/components/case-details/civil/children/Child.vue';
import { partyType } from '@/types/civil/jsonTypes';
import { faker } from '@faker-js/faker';
import { shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('Child.vue', () => {
  it('renders Child component', () => {
    const mockChild = {
      givenNm: faker.person.firstName(),
      lastNm: faker.person.lastName(),
    } as partyType;
    const wrapper = shallowMount(Child, {
      props: {
        child: mockChild,
      },
    });

    const chip = wrapper.find('v-chip');
    const labelWithTooltip = wrapper.findAllComponents({
      name: 'label-with-tooltip',
    });

    expect(chip.classes()).toContain('text-uppercase');
    expect(chip.text()).toBe(`${mockChild.lastNm}, ${mockChild.givenNm}`);
    expect(labelWithTooltip.length).toBe(1);
  });
});
