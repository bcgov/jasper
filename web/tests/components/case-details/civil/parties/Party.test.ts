import Party from '@/components/case-details/civil/parties/Party.vue';
import { partyType } from '@/types/civil/jsonTypes';
import { faker } from '@faker-js/faker';
import { shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('Party.vue', () => {
  it('renders Party component with correct details', () => {
    const mockParty = {
      givenNm: faker.person.firstName(),
      lastNm: faker.person.lastName(),
    } as partyType;
    const wrapper = shallowMount(Party, {
      props: {
        party: mockParty,
      },
    });

    const chip = wrapper.find('v-chip');
    const labelWithTooltip = wrapper.findAllComponents({
      name: 'label-with-tooltip',
    });

    expect(chip.classes()).toContain('text-uppercase');
    expect(chip.text()).toBe(`${mockParty.lastNm}, ${mockParty.givenNm}`);
    expect(labelWithTooltip.length).toBe(2);
  });
});
