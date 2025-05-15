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
      isCriminal: true,
    };
  });

  it('renders default card and table if no appearances are provided', () => {
    const wrapper = mount(AppearancesView, { props: { appearances: [] } });

    const card = wrapper.find('v-card');
    expect(card.exists()).toBe(true);
    expect(card.text()).toContain('Appearances');
    expect(wrapper.find('v-data-table-virtual').exists()).toBe(true);
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

  it('renders correct table headers for criminal appearances', () => {
    const wrapper: any = mount(AppearancesView, { props });

    const expectedHeaderKeys = [
      "data-table-expand",
      "appearanceDt",
      "DARS",
      "appearanceReasonCd",
      "appearanceTm",
      "courtLocation",
      "judgeFullNm",
      "name",
      "appearanceStatusCd",
    ];
    const headerKeys = wrapper.vm.pastHeaders.map((header: any) => header.key);

    expect(headerKeys).toEqual(expectedHeaderKeys);
  });

  it('renders correct table headers for civil appearances', () => {
    props.isCriminal = false;
    const wrapper: any = mount(AppearancesView, { props });

    const expectedHeaderKeys = [
      "data-table-expand",
      "appearanceDt",
      "DARS",
      "appearanceReasonCd",
      "appearanceTm",
      "courtLocation",
      "judgeFullNm",
      "appearanceStatusCd",
    ];
    const headerKeys = wrapper.vm.pastHeaders.map((header: any) => header.key);

    expect(headerKeys).toEqual(expectedHeaderKeys);
  });

  it.each([
    { isCriminal: true, shouldExist: true },
    { isCriminal: false, shouldExist: false },
  ])(
    'renders appearances filters based on isCriminal prop',
    ({ isCriminal, shouldExist }) => {
      props.isCriminal = isCriminal;
      const wrapper = mount(AppearancesView, { props });

      expect(wrapper.find('NameFilter').exists()).toBe(shouldExist);
    }
  );
});
