import { AdditionalProperties } from '@/types/common';
import {
  banType,
  criminalAppearancesType,
  criminalFileDetailsType,
  crownType,
} from '@/types/criminal/jsonTypes';
import { mount } from '@vue/test-utils';
import CriminalSummary from 'CMP/case-details/criminal/CriminalSummary.vue';
import { describe, expect, it } from 'vitest';

const createParticipant = (givenNm: string, lastNm: string) => ({
  ban: [],
  charge: [],
  count: [],
  fullName: '',
  document: [],
  hideJustinCounsel: false,
  partId: '',
  profSeqNo: '',
  lastNm,
  givenNm,
  orgNm: '',
  warrantYN: '',
  inCustodyYN: '',
  interpreterYN: '',
  detainedYN: '',
  birthDt: '',
  counselRrepId: '',
  counselPartId: '',
  counselLastNm: '',
  counselGivenNm: '',
  counselRelatedRepTypeCd: '',
  counselEnteredDt: '',
  designatedCounselYN: '',
  additionalProperties: {} as AdditionalProperties,
  additionalProp1: {},
  additionalProp2: {},
  additionalProp3: {},
});

const details: criminalFileDetailsType = {
  responseCd: '200',
  responseMessageTxt: 'Success',
  justinNo: '123456',
  fileNumberTxt: '123',
  homeLocationAgenId: 'H',
  homeLocationAgencyName: 'LocationA',
  homeLocationAgencyCode: '',
  homeLocationRegionName: '',
  courtLevelCd: '',
  courtLevelDescription: '',
  courtClassCd: '',
  courtClassDescription: '',
  activityClassCd: '',
  activityClassDesc: '',
  currentEstimateLenQty: '10',
  currentEstimateLenUnit: 'days',
  initialEstimateLenQty: '5',
  initialEstimateLenUnit: 'days',
  trialStartDt: '2023-01-01',
  mdocSubCategoryDsc: '',
  mdocCcn: '',
  assignedPartNm: '',
  approvedByAgencyCd: '',
  approvedByPartNm: '',
  approvalCrownAgencyTypeCd: '',
  crownEstimateLenQty: 15,
  crownEstimateLenDsc: '15 days',
  crownEstimateLenUnit: 'days',
  caseAgeDays: '100',
  indictableYN: 'Y',
  complexityTypeCd: '',
  assignmentTypeDsc: '',
  trialRemark: [],
  participant: [createParticipant('John', 'Doe')],
  witness: [],
  crown: [],
  hearingRestriction: [],
  appearances: {
    responseCd: '200',
    responseMessageTxt: 'Success',
    futureRecCount: '0',
    historyRecCount: '0',
    apprDetail: [],
    additionalProperties: {} as AdditionalProperties,
    additionalProp1: {},
    additionalProp2: {},
    additionalProp3: {},
  } as criminalAppearancesType,
};

describe('CriminalSummary.vue', () => {
  it('renders case details correctly', () => {
    const wrapper = mount(CriminalSummary, {
      props: { details },
    });

    expect(wrapper.find('h5').text()).toBe('Case Details');
    expect(wrapper.findComponent({ name: 'DivisionBadge' }).exists()).toBe(
      true
    );
    expect(wrapper.findComponent({ name: 'FileMarkers' }).exists()).toBe(true);
    expect(wrapper.find('v-card-title').text()).toBe('DOE, John');
    expect(wrapper.find('v-card-subtitle').text()).toBe('LocationA');
    expect(wrapper.findAll('v-row')?.at(1)?.find('v-col').text()).toBe('Crown');
    expect(
      wrapper.findAll('v-row')?.at(2)?.findAll('v-col')?.at(1)?.text()
    ).toBe('100 days');
  });

  it('renders correct text when more than one particpant', () => {
    details.participant = [
      createParticipant('John', 'Doe'),
      createParticipant('Charlie', 'Brown'),
    ];

    const wrapper = mount(CriminalSummary, {
      props: { details },
    });

    expect(wrapper.find('v-card-title').text()).toBe(
      'DOE, John and 1 other(s)'
    );
  });

  it('renders bans text when bans present', () => {
    details.participant[0].ban = [{} as banType];

    const wrapper = mount(CriminalSummary, {
      props: { details },
    });

    expect(wrapper.find('span').text()).toBe('BAN');
  });

  it.each([
    [true, 'Byrne, David'],
    [false, ''],
  ])(
    'renders crown names when crown present and assigned',
    (assigned, output) => {
      details.crown = [
        { givenNm: 'David', lastNm: 'Byrne', assigned: assigned } as crownType,
      ];

      const wrapper = mount(CriminalSummary, {
        props: { details },
      });

      expect(
        wrapper.findAll('v-row')?.at(1)?.findAll('v-col')?.at(1)?.text()
      ).toBe(output);
    }
  );

  it.each([
    ['Y', 'By Indictment'],
    ['N', 'Summarily'],
  ])('renders proceeded correctly', (indictableYN, output) => {
    details.indictableYN = indictableYN;

    const wrapper = mount(CriminalSummary, {
      props: { details },
    });

    expect(
      wrapper.findAll('v-row')?.at(0)?.findAll('v-col')?.at(1)?.text()
    ).toBe(output);
  });
});
