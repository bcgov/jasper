import SentenceOrderDetailsView from '@/components/case-details/criminal/sentence-order-details/SentenceOrderDetailsView.vue';
import { countType, criminalParticipantType } from '@/types/criminal/jsonTypes';
import { faker } from '@faker-js/faker';
import { shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import { nextTick } from 'vue';

describe('SentenceOrderDetailsView.vue', () => {
  it('renders sentence order details', () => {
    const wrapper = shallowMount(SentenceOrderDetailsView, {
      props: { participants: [{} as criminalParticipantType] },
    });

    const title = wrapper.find('v-card-text');
    const nameFilter = wrapper.findComponent({ name: 'name-filter' });
    const table = wrapper.findComponent({
      name: 'sentence-order-details-table',
    });
    const dialog = wrapper.findComponent({
      name: 'sentence-order-details-dialog',
    });
    const buttons = wrapper.findAll('v-btn-secondary');

    expect(title.text()).toBe('Sentence/Order Details');
    expect(nameFilter).not.toBeNull();
    expect(table).not.toBeNull();
    expect(dialog).not.toBeNull();
    expect(buttons.length).toBe(2);
  });

  it('renders correct count', () => {
    const wrapper = shallowMount(SentenceOrderDetailsView, {
      props: {
        participants: [
          {
            fullName: faker.person.fullName(),
            count: [{} as countType],
          } as criminalParticipantType,
        ],
      },
    });
    const count = wrapper.find('h5');

    expect(count.text()).toBe('Counts (1)');
  });

  it('clicking Order Made Details button opens the modal dialog and pass correct props', async () => {
    const wrapper = shallowMount(SentenceOrderDetailsView, {
      props: { participants: [{} as criminalParticipantType] },
    });

    const buttons = wrapper.findAll('v-btn-secondary');

    buttons[0].trigger('click');

    await nextTick();

    const dialog = wrapper.findComponent({
      name: 'sentence-order-details-dialog',
    });

    expect(dialog.attributes('title')).toBe('Order Made Details');
    expect(dialog.attributes('subtitle')).toBe('Conditions of Probation:');
    expect(dialog.attributes('targetproperty')).toBe('sentDetailTxt');
    expect(dialog.attributes('modelvalue')).toBe('true');
  });

  it('clicking Order Made Details button opens the modal dialog and pass correct props', async () => {
    const wrapper = shallowMount(SentenceOrderDetailsView, {
      props: { participants: [{} as criminalParticipantType] },
    });

    const buttons = wrapper.findAll('v-btn-secondary');
    buttons[0].trigger('click');
    await nextTick();

    const dialog = wrapper.findComponent({
      name: 'sentence-order-details-dialog',
    });

    expect(dialog.attributes('title')).toBe('Order Made Details');
    expect(dialog.attributes('subtitle')).toBe('Conditions of Probation:');
    expect(dialog.attributes('targetproperty')).toBe('sentDetailTxt');
    expect(dialog.attributes('modelvalue')).toBe('true');
  });

  it(`clicking Judge's Recommendation button opens the modal dialog and pass correct props`, async () => {
    const wrapper = shallowMount(SentenceOrderDetailsView, {
      props: { participants: [{} as criminalParticipantType] },
    });

    const buttons = wrapper.findAll('v-btn-secondary');
    buttons[1].trigger('click');
    await nextTick();

    const dialog = wrapper.findComponent({
      name: 'sentence-order-details-dialog',
    });

    expect(dialog.attributes('title')).toBe(`Judge's Recommendation`);
    expect(dialog.attributes('subtitle')).toBe('');
    expect(dialog.attributes('targetproperty')).toBe('judgesRecommendation');
    expect(dialog.attributes('modelvalue')).toBe('true');
  });
});
