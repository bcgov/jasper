import DivisionBadge from '@/components/case-details/common/DivisionBadge.vue';
import { CourtClassEnum } from '@/types/common';
import { getEnumName } from '@/utils/utils';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('DivisionBadge.vue', () => {
  const createWrapper = (
    division: string,
    activityClassDesc: string,
    courtClassCd: string
  ) => {
    return mount(DivisionBadge, {
      props: {
        division,
        activityClassDesc,
        courtClassCd,
      },
    });
  };

  it.each([
    {
      division: 'Criminal',
      activityClassDesc: 'Adult',
      courtClassCd: getEnumName(CourtClassEnum, CourtClassEnum.A),
      expectedText: 'Criminal - Adult',
      expectedStyleClass: 'criminal',
    },
    {
      division: 'Criminal',
      activityClassDesc: 'Youth Justice',
      courtClassCd: getEnumName(CourtClassEnum, CourtClassEnum.Y),
      expectedText: 'Criminal - Youth',
      expectedStyleClass: 'criminal',
    },
    {
      division: 'Small Claims',
      activityClassDesc: 'Small Claims',
      courtClassCd: getEnumName(CourtClassEnum, CourtClassEnum.C),
      expectedText: 'Small Claims',
      expectedStyleClass: 'small-claims',
    },
    {
      division: 'Motor Vehicle Accidents',
      activityClassDesc: 'Motor Vehicle Accidents',
      courtClassCd: getEnumName(CourtClassEnum, CourtClassEnum.M),
      expectedText: 'Motor Vehicle Accidents',
      expectedStyleClass: 'small-claims',
    },
    {
      division: 'Enforcement/Legislated Statute',
      activityClassDesc: 'Enforcement/Legislated Statute',
      courtClassCd: getEnumName(CourtClassEnum, CourtClassEnum.L),
      expectedText: 'Enforcement/Legislated Statute',
      expectedStyleClass: 'small-claims',
    },
    {
      division: 'Family',
      activityClassDesc: '',
      courtClassCd: getEnumName(CourtClassEnum, CourtClassEnum.F),
      expectedText: 'Family',
      expectedStyleClass: 'family',
    },
    {
      division: 'Unknown',
      activityClassDesc: '',
      courtClassCd: '',
      expectedText: 'Unknown',
      expectedStyleClass: 'unknown',
    },
  ])(
    'renders correctly with $division division and $activityClassDesc activity class',
    ({
      division,
      activityClassDesc,
      expectedText,
      expectedStyleClass,
      courtClassCd,
    }) => {
      const wrapper = createWrapper(division, activityClassDesc, courtClassCd);

      expect(wrapper.text()).toContain(expectedText);
      expect(wrapper.find('v-chip').classes()).toContain('text-uppercase');
      expect(wrapper.find('v-chip').classes()).toContain(expectedStyleClass);
    }
  );
});
