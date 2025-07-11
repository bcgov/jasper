import Party from '@/components/case-details/civil/parties/Party.vue';
import { partyAliasType, partyType } from '@/types/civil/jsonTypes';
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
    expect(labelWithTooltip.length).toBe(1);
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

  it('renders Party with alias data for Small Claims case', () => {
    const aliasLastName = faker.person.lastName();
    const aliasFirstName = faker.person.firstName();
    const aliasSecondName = faker.person.firstName();
    const aliasThirdName = faker.person.firstName();

    const mockParty = {
      givenNm: faker.person.firstName(),
      lastNm: faker.person.lastName(),
      aliases: [
        {
          surnameNm: aliasLastName,
          firstGivenNm: aliasFirstName,
          secondGivenNm: aliasSecondName,
          thirdGivenNm: aliasThirdName,
        } as partyAliasType,
      ],
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
    const alias = labelWithTooltip[0].attributes('values');

    expect(chip.classes()).toContain('text-uppercase');
    expect(chip.text()).toBe(`${mockParty.lastNm}, ${mockParty.givenNm}`);
    expect(labelWithTooltip.length).toBe(2);
    expect(alias).toBe(
      `${aliasLastName.toUpperCase()}, ${aliasFirstName} ${aliasSecondName} ${aliasThirdName}`
    );
  });

  it('renders Party with alias data for MVA case', () => {
    const aliasLastName = faker.person.lastName();
    const aliasFirstName = faker.person.firstName();

    const mockParty = {
      givenNm: faker.person.firstName(),
      lastNm: faker.person.lastName(),
      aliases: [
        {
          surnameNm: aliasLastName,
          firstGivenNm: aliasFirstName,
        } as partyAliasType,
      ],
    } as partyType;
    const wrapper = shallowMount(Party, {
      props: {
        party: mockParty,
        courtClassCd: CourtClassEnum[CourtClassEnum.M],
      },
    });

    const chip = wrapper.find('v-chip');
    const labelWithTooltip = wrapper.findAllComponents({
      name: 'label-with-tooltip',
    });
    const alias = labelWithTooltip[0].attributes('values');

    expect(chip.classes()).toContain('text-uppercase');
    expect(chip.text()).toBe(`${mockParty.lastNm}, ${mockParty.givenNm}`);
    expect(labelWithTooltip.length).toBe(2);
    expect(alias).toBe(`${aliasLastName.toUpperCase()}, ${aliasFirstName}`);
  });

  it('renders Party with alias of an org name data for Legislated case', () => {
    const aliasCompanyName = faker.company.name();

    const mockParty = {
      givenNm: faker.person.firstName(),
      lastNm: faker.person.lastName(),
      aliases: [
        {
          organizationNm: aliasCompanyName,
        } as partyAliasType,
      ],
    } as partyType;
    const wrapper = shallowMount(Party, {
      props: {
        party: mockParty,
        courtClassCd: CourtClassEnum[CourtClassEnum.L],
      },
    });

    const chip = wrapper.find('v-chip');
    const labelWithTooltip = wrapper.findAllComponents({
      name: 'label-with-tooltip',
    });
    const alias = labelWithTooltip[0].attributes('values');

    expect(chip.classes()).toContain('text-uppercase');
    expect(chip.text()).toBe(`${mockParty.lastNm}, ${mockParty.givenNm}`);
    expect(labelWithTooltip.length).toBe(2);
    expect(alias).toBe(aliasCompanyName);
  });

  it('renders Party without alias of an org name data for Legislated case', () => {
    const aliasCompanyName = faker.company.name();

    const mockParty = {
      givenNm: faker.person.firstName(),
      lastNm: faker.person.lastName(),
    } as partyType;
    const wrapper = shallowMount(Party, {
      props: {
        party: mockParty,
        courtClassCd: CourtClassEnum[CourtClassEnum.L],
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
});
