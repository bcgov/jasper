import DivisionBadge from '@/components/case-details/common/DivisionBadge.vue';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('DivisionBadge.vue', () => {
  const createWrapper = (division: string, activityClassDesc: string) => {
    return mount(DivisionBadge, {
      props: {
        division,
        activityClassDesc,
      },
    });
  };

  it.each([
    {
      division: 'Criminal',
      activityClassDesc: 'Adult',
      expectedText: 'CRIMINAL - ADULT',
      expectedColor: '#4092c1',
    },
    {
      division: 'Criminal',
      activityClassDesc: 'Youth Justice',
      expectedText: 'CRIMINAL - YOUTH',
      expectedColor: '#4092c1',
    },
    {
      division: 'Family',
      activityClassDesc: '',
      expectedText: 'FAMILY',
      expectedColor: '#2e8540',
    },
    {
      division: 'Unknown',
      activityClassDesc: '',
      expectedText: 'UNKNOWN',
      expectedColor: '#79368f',
    },
  ])(
    'renders correctly with $division division and $activityClassDesc activity class',
    ({ division, activityClassDesc, expectedText, expectedColor }) => {
      const wrapper = createWrapper(division, activityClassDesc);
      expect(wrapper.text()).toContain(expectedText);
      expect(wrapper.find('v-chip').attributes('color')).toEqual(expectedColor);
    }
  );
});
