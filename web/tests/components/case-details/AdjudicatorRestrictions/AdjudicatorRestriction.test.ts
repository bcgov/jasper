import AdjudicatorRestriction from '@/components/case-details/AdjudicatorRestrictions/AdjudicatorRestriction.vue';
import { AdjudicatorRestrictionsInfoType } from '@/types/common';
import { faker } from '@faker-js/faker';
import { shallowMount } from '@vue/test-utils';
import { beforeEach, describe, expect, it } from 'vitest';

describe('AdjudicatorRestriction.vue', () => {
  let restrictionMock: AdjudicatorRestrictionsInfoType;

  beforeEach(() => {
    restrictionMock = {
      adjRestriction: faker.lorem.word(),
      adjudicator: faker.person.fullName(),
      fullName: faker.person.fullName(),
      status: faker.lorem.word(),
      appliesTo: faker.lorem.paragraph(),
    };
  });

  it('renders "SEIZED" status with correct class', () => {
    restrictionMock.status = 'seized';
    const wrapper = shallowMount(AdjudicatorRestriction, {
      props: {
        restriction: restrictionMock,
      },
    });

    expect(wrapper.find('b').classes()).toContain('seized');
    expect(wrapper.find('b').classes()).toContain('text-uppercase');
  });

  it('renders "ASSIGNED" status with correct class', () => {
    restrictionMock.status = 'assIgNed';
    const wrapper = shallowMount(AdjudicatorRestriction, {
      props: {
        restriction: restrictionMock,
      },
    });

    expect(wrapper.find('b').classes()).toContain('assigned');
    expect(wrapper.find('b').classes()).toContain('text-uppercase');
  });

  it('renders "DISQUALIFIED" status with correct class', () => {
    restrictionMock.status = ' disqualiFied ';
    const wrapper = shallowMount(AdjudicatorRestriction, {
      props: {
        restriction: restrictionMock,
      },
    });

    expect(wrapper.find('b').classes()).toContain('disqualified');
    expect(wrapper.find('b').classes()).toContain('text-uppercase');
  });
  it('renders "APPEALED" status with correct class', () => {
    restrictionMock.status = 'appealed';
    const wrapper = shallowMount(AdjudicatorRestriction, {
      props: {
        restriction: restrictionMock,
      },
    });

    expect(wrapper.find('b').classes()).toContain('appealed');
    expect(wrapper.find('b').classes()).toContain('text-uppercase');
    expect(wrapper.find('p').text()).toBe(restrictionMock.appliesTo);
  });
});
