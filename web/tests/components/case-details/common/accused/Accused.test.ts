import Accused from '@/components/case-details/common/accused/Accused.vue';
import { criminalParticipantType } from '@/types/criminal/jsonTypes';
import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it } from 'vitest';

describe('Accused.vue', () => {
  const ageNoticeMock = [
    {
      eventDate: "2022-08-10 00:00:00.0",
      eventTypeDsc: "Notice to",
      noticeTo: "Garrett Green (social worker)",
      DOB: "",
      relationship: "",
      provenBy: "",
      detailText: ""
    },
    {
      eventDate: "2022-08-10 00:00:00.0",
      eventTypeDsc: "Proof of Age",
      noticeTo: "",
      DOB: "2005-02-25 00:00:00.0",
      relationship: "SOWORKER",
      provenBy: "Garrett GREEN",
      detailText: ""
    }
  ];
  const accusedMock = {
    lastNm: 'Doe',
    givenNm: 'John',
    ban: [
      { banTypeDescription: 'Type A' },
      { banTypeDescription: 'Type A' },
      { banTypeDescription: 'Type B' },
    ],
    birthDt: '1990-01-01 00:00:00.0',
    counselLastNm: 'Smith',
    counselGivenNm: 'Jane',
    designatedCounselYN: 'Yes',
    ageNotice: ageNoticeMock
  } as criminalParticipantType;

  const appearancesMock = [{}, {}, {}];

  let wrapper: any;

  beforeEach(() => {
    wrapper = mount(Accused, {
      props: { accused: accusedMock, appearances: appearancesMock, courtClassCd: 'A' },
    });
  });

  it('renders accused name in uppercase', () => {
    const chipText = wrapper.find('v-chip').text();

    expect(chipText).toContain('DOE, JOHN');
  });

  it('renders ban information correctly', () => {
    const banElements = wrapper.findAll('v-col > div > span');

    expect(banElements).toHaveLength(2);
    expect(banElements[0].text()).toContain('Type A (2)');
    expect(banElements[1].text()).toContain('Type B (1)');
  });

  it('shows ban modal when icon is clicked', async () => {
    const icon = wrapper.find('v-icon');
    expect(icon.exists()).toBe(true);

    await icon.trigger('click');
    expect(wrapper.findComponent({ name: 'Bans' }).exists()).toBe(true);
  });

  it('renders formatted DOB', () => {
    const dobText = wrapper.findAll('v-row')[3].find('v-col:last-child').text();
    expect(dobText).toBe('01-Jan-1990');
  });

  it('renders counsel last name in uppercase', () => {
    const counselText = wrapper
      .findAll('v-row')[4]
      .find('v-col:last-child')
      .text();
    expect(counselText).toBe('SMITH, Jane');
  });

  it('renders designated counsel status', () => {
    const counselStatus = wrapper
      .findAll('v-row')[5]
      .find('v-col:last-child')
      .text();
    expect(counselStatus).toBe('Yes');
  });

  it('renders appearances count', () => {
    const appearancesCount = wrapper
      .findAll('v-row')[6]
      .find('v-col:last-child')
      .text();
    expect(appearancesCount).toBe('3');
  });

  it('renders file markers', () => {
    const fileMarkers = wrapper.findComponent({ name: 'FileMarkers' });

    expect(Object.getPrototypeOf(fileMarkers)).not.toBe(null);
  });

  it('does not render Age/Notice when non youth file', () => {
    const ageNoticeLabel = wrapper.findAll('v-col.data-label').filter(col => col.text() === 'Age/Notice');
    expect(ageNoticeLabel.length).toBe(0);
  });

  it('renders Age/Notice when youth file', () => {
      wrapper = mount(Accused, {
        props: { accused: accusedMock, appearances: appearancesMock, courtClassCd: 'Y' },
      });
      const ageNoticeLabel = wrapper.findAll('v-col.data-label').filter(col => col.text() === 'Age/Notice');
      expect(ageNoticeLabel.length).toBe(1);
  });

    it('computes Age/Notice as Yes', () => {
      expect(wrapper.vm.ageNotice).toEqual('Yes');
    });

    it('computes Age/Notice as No', () => {
      ageNoticeMock[0].eventTypeDsc = 'Nope';
      wrapper = mount(Accused, {
        props: { accused: accusedMock, appearances: appearancesMock, courtClassCd: 'Y' },
      });
      expect(wrapper.vm.ageNotice).toEqual('No');
    });
});
