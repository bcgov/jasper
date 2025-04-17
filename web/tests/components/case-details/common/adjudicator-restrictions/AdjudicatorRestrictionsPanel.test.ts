import AdjudicatorRestrictionsPanel from '@/components/case-details/common/adjudicator-restrictions/AdjudicatorRestrictionsPanel.vue';
import { AdjudicatorRestrictionsInfoType } from '@/types/common';
import { faker } from '@faker-js/faker';
import { shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('AdjudicatorRestrictionsPanel.vue', () => {
  const restrictionsMock: AdjudicatorRestrictionsInfoType[] = [
    {
      adjRestriction: faker.lorem.word(),
      adjudicator: faker.person.fullName(),
      fullName: faker.person.fullName(),
      status: faker.lorem.word(),
      appliesTo: faker.lorem.paragraph(),
    },
    {
      adjRestriction: faker.lorem.word(),
      adjudicator: faker.person.fullName(),
      fullName: faker.person.fullName(),
      status: faker.lorem.word(),
      appliesTo: faker.lorem.paragraph(),
    },
  ];

  it('renders the correct Adjudicator Restrictions', () => {
    const wrapper = shallowMount(AdjudicatorRestrictionsPanel, {
      props: {
        adjudicatorRestrictions: restrictionsMock,
      },
    });

    expect(wrapper.find('h5').text()).toBe(
      `Adjudicator Restrictions (${restrictionsMock.length})`
    );
  });

  it('renders the correct number of Adjudicator Restriction components', () => {
    const wrapper = shallowMount(AdjudicatorRestrictionsPanel, {
      props: {
        adjudicatorRestrictions: restrictionsMock,
      },
    });

    const arComponents = wrapper.findAllComponents({
      name: 'adjudicator-restriction',
    });
    expect(arComponents).toHaveLength(restrictionsMock.length);
  });
});
