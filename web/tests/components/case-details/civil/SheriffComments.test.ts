import SheriffCommentsDialog from '@/components/case-details/civil/SheriffCommentsDialog.vue';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import { nextTick } from 'vue';

describe('SheriffCommentsDialog.vue', () => {
  it('renders dialog when modelValue is true', () => {
    const fakeComments = faker.lorem.paragraph(5);
    const wrapper = mount(SheriffCommentsDialog, {
      props: {
        comments: fakeComments,
        modelValue: true,
      },
    });

    const dialog = wrapper.find('v-dialog');
    const isShown = dialog.element.getAttribute('modelvalue') === 'true';

    expect(dialog.exists()).toBe(true);
    expect(isShown).toBe(true);
    expect(wrapper.find('v-card-text').text()).toBe(fakeComments);
  });

  it('hides dialog when close button is clicked', async () => {
    const fakeComments = faker.lorem.paragraph(5);
    const wrapper = mount(SheriffCommentsDialog, {
      props: {
        comments: fakeComments,
        modelValue: true,
      },
    });

    const button = wrapper.find('v-btn-secondary');
    button.trigger('click');

    await nextTick();

    const dialog = wrapper.find('v-dialog');
    const isShown = dialog.element.getAttribute('modelvalue') === 'true';

    expect(isShown).toBeFalsy();
  });
});
