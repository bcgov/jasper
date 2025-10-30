import { mount } from '@vue/test-utils';
import { describe, it, expect } from 'vitest';
import CourtListTableSearch from 'CMP/courtlist/CourtListTableSearch.vue';

describe('CourtListTableSearch.vue', () => {
    it.each([
      [true, 'To be called'],
      [false, 'Complete']
    ])('mounts component with expected selectedFiles value', async (isFuture, expectedValue) => {
        const wrapper = mount(CourtListTableSearch, {
          props: {
            isFuture
          }
        });

        expect(wrapper.vm.selectedFiles).toEqual(expectedValue);
    });

    it('resets filters when reset button is clicked', async () => {
        const wrapper = mount(CourtListTableSearch);
        await wrapper.find('v-btn').trigger('click');

        expect(wrapper.vm.selectedFiles).toEqual('Complete');
        expect(wrapper.vm.selectedAMPM).toBeUndefined();
        expect(wrapper.vm.search).toBeUndefined();
    });

    it.each([
      [true, 'To be called'],
      [false, 'Complete']
    ])('resets selectedFiles with expected value when reset button is clicked', async (isFuture, expectedValue) => {
        const wrapper = mount(CourtListTableSearch, {
          props: {
            isFuture
          }
        });

        wrapper.vm.selectedFiles = '';
        await wrapper.find('v-btn').trigger('click');

        expect(wrapper.vm.selectedFiles).toEqual(expectedValue);
    });
});