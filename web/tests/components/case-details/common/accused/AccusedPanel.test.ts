import AccusedPanel from '@/components/case-details/common/accused/AccusedPanel.vue';
import { CourtClassEnum } from '@/types/common';
import {
  criminalApprDetailType,
  criminalParticipantType,
} from '@/types/criminal/jsonTypes';
import { getEnumName } from '@/utils/utils';
import { shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('AccusedPanel.vue', () => {
  const accusedMock = [
    { partId: 1, lastNm: 'Smith' } as unknown as criminalParticipantType,
    { partId: 2, lastNm: 'Johnson' } as unknown as criminalParticipantType,
  ];
  const appearancesMock = [
    {
      lastNm: 'Smith',
      details: 'Appearance 1',
    } as unknown as criminalApprDetailType,
    {
      lastNm: 'Johnson',
      details: 'Appearance 2',
    } as unknown as criminalApprDetailType,
    {
      lastNm: 'Smith',
      details: 'Appearance 3',
    } as unknown as criminalApprDetailType,
  ];

  it.each([
    [getEnumName(CourtClassEnum, CourtClassEnum.A), 'Accused'],
    [getEnumName(CourtClassEnum, CourtClassEnum.Y), 'Youth'],
    [getEnumName(CourtClassEnum, CourtClassEnum.T), 'Participants'],
  ])(
    'renders the correct title for the current court class',
    (courtClassCd, output) => {
      const wrapper = shallowMount(AccusedPanel, {
        props: {
          accused: accusedMock,
          courtClassCd: courtClassCd,
          appearances: appearancesMock,
        },
      });
      expect(wrapper.find('h5').text()).toBe(
        `${output} (${accusedMock.length})`
      );
    }
  );

  it('renders the correct number of Accused components', () => {
    const wrapper = shallowMount(AccusedPanel, {
      props: {
        accused: accusedMock,
        courtClassCd: getEnumName(CourtClassEnum, CourtClassEnum.A),
        appearances: appearancesMock,
      },
    });
    const accusedComponents = wrapper.findAllComponents({ name: 'Accused' });
    expect(accusedComponents).toHaveLength(accusedMock.length);
  });
});
