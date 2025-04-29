import Party from '@/components/case-details/civil/parties/Party.vue';
import { partyType } from '@/types/civil/jsonTypes';
import { CourtClassEnum } from '@/types/common';
import { faker } from '@faker-js/faker';
import { shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('Party.vue', () => {
  it('renders Party component for Small Claims case detail', () => {
    const mockParty = {
      givenNm: faker.person.firstName(),
      lastNm: faker.person.lastName(),
    } as partyType;
    const wrapper = shallowMount(Party, {
      props: {
        party: mockParty,
        courtClassCd: CourtClassEnum[CourtClassEnum.C],
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

  it('renders Party component for Family case detail', () => {
    const mockParty = {
      givenNm: faker.person.firstName(),
      lastNm: faker.person.lastName(),
    } as partyType;
    const wrapper = shallowMount(Party, {
      props: {
        party: mockParty,
        courtClassCd: CourtClassEnum[CourtClassEnum.F],
      },
    });

    const chip = wrapper.find('v-chip');
    const labelWithTooltip = wrapper.findAllComponents({
      name: 'label-with-tooltip',
    });

    expect(chip.classes()).toContain('text-uppercase');
    expect(chip.text()).toBe(`${mockParty.lastNm}, ${mockParty.givenNm}`);
    expect(labelWithTooltip.length).toBe(1);
  });

  it('renders Party component with "Self-Represented" Counsel', () => {
    const mockParty = {
      givenNm: faker.person.firstName(),
      lastNm: faker.person.lastName(),
      selfRepresentedYN: 'Y',
    } as partyType;
    const wrapper = shallowMount(Party, {
      props: {
        party: mockParty,
        courtClassCd: CourtClassEnum[CourtClassEnum.F],
      },
    });

    const rows = wrapper.findAll('v-row');
    const lastRow = rows[rows.length - 1];
    const label = lastRow.findComponent({ name: 'LabelWithTooltip' });

    expect(label.attributes('values')).toBe('Self-Represented');
  });
});
