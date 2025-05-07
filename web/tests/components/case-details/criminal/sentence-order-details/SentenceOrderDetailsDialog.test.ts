import SentenceOrderDetailsDialog from '@/components/case-details/criminal/sentence-order-details/SentenceOrderDetailsDialog.vue';
import {
  countSentenceType,
  countType,
  criminalParticipantType,
} from '@/types/criminal/jsonTypes';
import { faker } from '@faker-js/faker';
import { shallowMount } from '@vue/test-utils';
import { beforeEach, describe, expect, it } from 'vitest';
import { nextTick } from 'vue';

describe('SentenceOrderDetailsDialog', () => {
  let mockProps;
  const fakeTitle = faker.lorem.words(3);
  const fakeSubtitle = faker.lorem.words(5);
  const fakeTargetProperty = 'sentDetailTxt';

  beforeEach(() => {
    mockProps = {
      title: fakeTitle,
      subtitle: fakeSubtitle,
      targetProperty: fakeTargetProperty,
      participants: [] as criminalParticipantType[],
      modelValue: true,
    };
  });

  it('renders dialog values from props', () => {
    mockProps.participants = [
      {
        fullName: faker.person.fullName(),
        count: [
          {
            sentence: [
              {
                sentDetailTxt: faker.lorem.paragraph(),
              } as countSentenceType,
              {
                sentDetailTxt: faker.lorem.paragraph(),
              } as countSentenceType,
            ],
          } as countType,
        ],
      } as criminalParticipantType,
    ];

    const wrapper = shallowMount(SentenceOrderDetailsDialog, {
      props: mockProps,
    });

    const title = wrapper.find('v-card-title');
    const subtitle = wrapper.find('v-card-text p');
    const comments = wrapper.findAll('li');

    expect(title.text()).toBe(fakeTitle);
    expect(Object.getPrototypeOf(subtitle)).not.toBe(null);
    expect(subtitle.text()).toBe(fakeSubtitle);
    expect(comments.length).toBe(2);
  });

  it('does not render subtitle when it is falsy', () => {
    mockProps.subtitle = '';
    const wrapper = shallowMount(SentenceOrderDetailsDialog, {
      props: mockProps,
    });

    const title = wrapper.find('v-card-title');
    const subtitle = wrapper.find('v-card-text p');

    expect(title.text()).toBe(fakeTitle);
    expect(Object.getPrototypeOf(subtitle)).toBe(null);
  });

  it('sets selectedAccused as blank when dialog is closed', async () => {
    const wrapper = shallowMount(SentenceOrderDetailsDialog, {
      props: mockProps,
    });

    const closeButton = wrapper.find('v-btn-secondary');
    closeButton.trigger('click');
    await nextTick();

    expect(wrapper.vm.selectedAccused).toBe('');
  });
});
