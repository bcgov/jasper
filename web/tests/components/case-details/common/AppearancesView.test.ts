import { mount } from '@vue/test-utils';
import AppearancesView from 'CMP/case-details/common/AppearancesView.vue';
import { beforeEach, describe, expect, it } from 'vitest';

describe('AppearancesView.vue', () => {
  let props: any;

  beforeEach(() => {
    props = {
      appearances: [
        {
          appearanceId: 1,
          appearanceDt: '2023-10-01',
          appearanceReasonCd: 'ARR',
          appearanceReasonDsc: 'Arraignment',
          appearanceTm: '10:00',
          estimatedTimeHour: 1,
          estimatedTimeMin: 30,
          courtLocation: 'Court A',
          courtRoomCd: '101',
          judgeFullNm: 'Judge Smith',
          lastNm: 'Doe',
          givenNm: 'John',
          appearanceStatusCd: 'Scheduled',
          activity: 'Hearing',
        },
        {
          appearanceId: 2,
          appearanceDt: '3023-10-01',
          appearanceReasonCd: 'TRI',
          appearanceReasonDsc: 'Trial',
          appearanceTm: '14:00',
          estimatedTimeHour: 2,
          estimatedTimeMin: 0,
          courtLocation: 'Court B',
          courtRoomCd: '202',
          judgeFullNm: 'Judge Brown',
          lastNm: 'Smith',
          givenNm: 'Jane',
          appearanceStatusCd: 'Completed',
          activity: 'Trial',
        },
      ],
    };
  });

  it('does not render cards or tables if no appearances are provided', () => {
    const wrapper = mount(AppearancesView, { props: { appearances: [] } });

    expect(wrapper.find('v-card').exists()).toBe(false);
    expect(wrapper.find('v-data-table-virtual').exists()).toBe(false);
  });

  it('renders only past appearances', () => {
    props.appearances[1].appearanceDt = '1999-10-01';
    const wrapper = mount(AppearancesView, { props });

    expect(wrapper.findAll('v-card').length).toBe(1);
    expect(
      wrapper.findAll('v-card')?.at(0)?.findAll('v-col')?.at(0)?.text()
    ).toBe('Past Appearances');
  });

  it('renders only future appearances', () => {
    props.appearances[0].appearanceDt = '3030-10-01';
    const wrapper = mount(AppearancesView, { props });

    expect(wrapper.findAll('v-card').length).toBe(1);
    expect(
      wrapper.findAll('v-card')?.at(0)?.findAll('v-col')?.at(0)?.text()
    ).toBe('Future Appearances');
  });

  it('renders both past and future appearances', () => {
    const wrapper = mount(AppearancesView, { props });

    expect(
      wrapper.findAll('v-card')?.at(0)?.findAll('v-col')?.at(0)?.text()
    ).toBe('Past Appearances');
    expect(
      wrapper.findAll('v-card')?.at(1)?.findAll('v-col')?.at(0)?.text()
    ).toBe('Future Appearances');
  });

  it('filters appearances by selected accused', () => {
    const wrapper: any = mount(AppearancesView, { props });

    wrapper.vm.selectedAccused = 'Doe, John';
    const appearances = wrapper.vm.pastAppearances.concat(
      wrapper.vm.futureAppearances
    );
    expect(appearances.length).toBe(1);
  });
});
