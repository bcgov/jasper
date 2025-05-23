import CivilSummary from '@/components/case-details/civil/CivilSummary.vue';
import { civilFileDetailsType } from '@/types/civil/jsonTypes';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('CivilSummary.vue', () => {
  it('renders civil case details correctly', () => {
    const fakeStyleOfCause = faker.person.fullName();
    const fakeLocation = faker.location.city();
    const wrapper = mount(CivilSummary, {
      props: {
        details: {
          socTxt: fakeStyleOfCause,
          homeLocationAgencyName: fakeLocation,
        } as civilFileDetailsType,
      },
    });
    expect(wrapper.findComponent({ name: 'DivisionBadge' }).exists()).toBe(
      true
    );
    expect(wrapper.find('v-card-title').text()).toBe(fakeStyleOfCause);
    expect(wrapper.find('v-card-subtitle').text()).toBe(fakeLocation);
    expect(wrapper.find('v-icon').exists()).toBe(false);
    expect(wrapper.find('v-dialog').exists()).toBe(false);
  });

  it('renders civil case details with sheriff comments', async () => {
    const fakeComments = faker.lorem.paragraph(5);
    const wrapper = mount(CivilSummary, {
      props: {
        details: {
          sheriffCommentText: fakeComments,
        } as civilFileDetailsType,
      },
    });

    expect(wrapper.find('v-icon').exists()).toBe(true);
    expect(wrapper.find('v-dialog').exists()).toBe(true);
    expect(wrapper.findAll('v-chip')).toHaveLength(1);
  });

  it('renders 2 chips to represent Division/CourtClass and CPA', async () => {
    const wrapper = mount(CivilSummary, {
      props: {
        details: {
          cfcsaFileYN: 'Y',
        } as civilFileDetailsType,
      },
    });

    expect(wrapper.findAll('v-chip')).toHaveLength(2);
  });
});
