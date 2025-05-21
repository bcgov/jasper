import Child from '@/components/case-details/civil/children/Child.vue';
import { partyType } from '@/types/civil/jsonTypes';
import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
import { faker } from '@faker-js/faker';
import { mount, shallowMount } from '@vue/test-utils';
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

  const formatDate = (
    date: Date,
    options?: { years?: number; months?: number; days?: number }
  ): string => {
    const pad = (n: number, width = 2) => n.toString().padStart(width, '0');

    const adjustedDate = new Date(date);

    if (options?.years) {
      adjustedDate.setFullYear(adjustedDate.getFullYear() + options.years);
    }

    if (options?.months) {
      adjustedDate.setMonth(adjustedDate.getMonth() + options.months);
    }

    if (options?.days) {
      adjustedDate.setDate(adjustedDate.getDate() + options.days);
    }

    const year = adjustedDate.getFullYear();
    const month = pad(adjustedDate.getMonth() + 1);
    const day = pad(adjustedDate.getDate());

    const hours = pad(adjustedDate.getHours());
    const minutes = pad(adjustedDate.getMinutes());
    const seconds = pad(adjustedDate.getSeconds());

    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}.0`;
  };

  it.each([
    ['1985-10-12 00:00:00.0', '39'],
    [null, ''],
    [formatDate(new Date()), '0 months'],
    [formatDate(new Date(), { months: -5 }), '5 months'],
    [formatDate(new Date(), { days: -2 }), '0 months'],
    [formatDate(new Date(), { years: -2 }), '2'],
  ])('renders correct age', (birthDate, expectedAge) => {
    const wrapper = mount(Child, {
      props: {
        child: {
          birthDate,
        } as partyType,
      },
    });

    const rows = wrapper.findAll('v-row');
    expect(rows).toHaveLength(4);

    const ageColumns = rows[1].findAll('v-col');
    expect(ageColumns).toHaveLength(2);

    const ageValue = ageColumns[1].text();
    expect(ageValue).toBe(expectedAge);

    const birthDateColumns = rows[2].findAll('v-col');
    expect(birthDateColumns).toHaveLength(2);

    const formattedBirthDate = birthDateColumns[1].text();
    expect(formattedBirthDate).toBe(formatDateToDDMMMYYYY(birthDate!));
  });
});
