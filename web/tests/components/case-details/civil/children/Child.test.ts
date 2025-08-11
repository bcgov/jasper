import Child from '@/components/case-details/civil/children/Child.vue';
import { partyType } from '@/types/civil/jsonTypes';
import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
import { faker } from '@faker-js/faker';
import { mount, shallowMount } from '@vue/test-utils';
import { DateTime } from 'luxon';
import { afterAll, beforeAll, describe, expect, it, vi } from 'vitest';

describe('Child.vue', () => {
  beforeAll(() => {
    vi.useFakeTimers();
    // Set a fixed date for consistent test results
    vi.setSystemTime(new Date('2025-08-08T00:00:00-07:00'));
  });

  afterAll(() => {
    vi.useRealTimers();
  });

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

  const formatDate = (dateStr: string) =>
    DateTime.fromFormat(dateStr, 'yyyy-MM-dd').toFormat(
      'yyyy-MM-dd HH:mm:ss.S'
    );

  // Current date is fixed to Aug 8, 2025 in beforeAll
  it.each([
    ['1985-10-12 00:00:00.0', '39'], // 39 years old
    [null, ''],
    [formatDate('2025-08-06'), '0 months'], // 2 days old
    [formatDate('2025-03-06'), '5 months'], // 5 months old
    [formatDate('2025-08-04'), '0 months'], // 4 days old
    [formatDate('2023-08-06'), '2'], // 2 years old and 2 days
    [formatDate('2024-08-28'), '11 months'], // 11 months old and 20 days
    [formatDate('2024-08-06'), '1'], // 1 year old and 2 days
    [formatDate('2023-08-01'), '2'], // 2 years old and 7 days
    [formatDate('2023-08-31'), '1'], // 1 year and 11 months old
    [formatDate('2024-08-09'), '11 months'], // 11 months and 30 days old
    [formatDate('2023-08-07'), '2'], // 2 years old and 1 day
    [formatDate('2024-08-07'), '1'], // 1 year old and 1 day
    [formatDate('2025-08-07'), '0 months'], // 1 day old
    [formatDate('2024-08-08'), '1'], // 1 year old (birthday)
    [formatDate('2020-02-29'), '5'], // 5 years old (leap year)
    [formatDate('2024-02-29'), '1'], // 1 year old and 5 months (leap year)
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

    // The current date has to be fixed to avoid issue since dates are relative to the current date
    const ageValue = ageColumns[1].text();
    expect(ageValue).toBe(expectedAge);

    const birthDateColumns = rows[2].findAll('v-col');
    expect(birthDateColumns).toHaveLength(2);

    const formattedBirthDate = birthDateColumns[1].text();
    expect(formattedBirthDate).toBe(formatDateToDDMMMYYYY(birthDate!));
  });
});
