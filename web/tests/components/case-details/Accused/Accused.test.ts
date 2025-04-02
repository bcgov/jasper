import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import Accused from 'CMP/case-details/Accused/Accused.vue';

describe('Accused.vue', () => {
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
  };

  const appearancesMock = [{}, {}, {}];

  it('renders accused name in uppercase', () => {
    const wrapper = mount(Accused, {
      props: { accused: accusedMock, appearances: appearancesMock },
    });

    const chipText = wrapper.find('v-chip').text();
    expect(chipText).toContain('DOE, JOHN');
  });

  it('renders ban information correctly', () => {
    const wrapper = mount(Accused, {
      props: { accused: accusedMock, appearances: appearancesMock },
    });

    const banElements = wrapper.findAll('v-col > div > span');
    expect(banElements).toHaveLength(2);
    expect(banElements[0].text()).toContain('Type A (2)');
    expect(banElements[1].text()).toContain('Type B (1)');
  });

  it('shows ban modal when icon is clicked', async () => {
    const wrapper = mount(Accused, {
      props: { accused: accusedMock, appearances: appearancesMock },
    });

    const icon = wrapper.find('v-icon');
    expect(icon.exists()).toBe(true);

    await icon.trigger('click');
    expect(wrapper.findComponent({ name: 'Bans' }).exists()).toBe(true);
  });

  it('renders formatted DOB', () => {
    const wrapper = mount(Accused, {
      props: { accused: accusedMock, appearances: appearancesMock },
    });

    const dobText = wrapper.findAll('v-row')[2].find('v-col:last-child').text();
    expect(dobText).toBe('01-Jan-1990');
  });

  it('renders counsel last name in uppercase', () => {
    const wrapper = mount(Accused, {
      props: { accused: accusedMock, appearances: appearancesMock },
    });

    const counselText = wrapper.findAll('v-row')[3].find('v-col:last-child').text();
    expect(counselText).toBe('SMITH, Jane');
  });

  it('renders designated counsel status', () => {
    const wrapper = mount(Accused, {
      props: { accused: accusedMock, appearances: appearancesMock },
    });

    const counselStatus = wrapper.findAll('v-row')[4].find('v-col:last-child').text();
    expect(counselStatus).toBe('Yes');
  });

  it('renders appearances count', () => {
    const wrapper = mount(Accused, {
      props: { accused: accusedMock, appearances: appearancesMock },
    });

    const appearancesCount = wrapper.findAll('v-row')[5].find('v-col:last-child').text();
    expect(appearancesCount).toBe('3');
  });
});