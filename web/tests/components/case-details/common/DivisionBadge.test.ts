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
      expectedText: 'Criminal - Adult',
      expectedStyleClass: 'criminal',
    },
    {
      division: 'Criminal',
      activityClassDesc: 'Youth Justice',
      expectedText: 'Criminal - Youth',
      expectedStyleClass: 'criminal',
    },
    {
      division: 'Family',
      activityClassDesc: '',
      expectedText: 'Family',
      expectedStyleClass: 'family',
    },
    {
      division: 'Unknown',
      activityClassDesc: '',
      expectedText: 'Unknown',
      expectedStyleClass: 'unknown',
    },
  ])(
    'renders correctly with $division division and $activityClassDesc activity class',
    ({ division, activityClassDesc, expectedText, expectedStyleClass }) => {
      const wrapper = createWrapper(division, activityClassDesc);

      expect(wrapper.text()).toContain(expectedText);
      expect(wrapper.find('v-chip').classes()).toContain('text-uppercase');
      expect(wrapper.find('v-chip').classes()).toContain(expectedStyleClass);
    }
  );
});
