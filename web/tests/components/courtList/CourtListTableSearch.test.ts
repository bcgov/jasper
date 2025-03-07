import { mount } from '@vue/test-utils';
import { describe, it, expect } from 'vitest';
import CourtListTableSearch from 'CMP/courtlist/CourtListTableSearch.vue';

describe('CourtListTableSearch.vue', () => {
    it('resets filters when reset button is clicked', async () => {
        const wrapper = mount(CourtListTableSearch);
        await wrapper.find('v-btn').trigger('click');

        expect(wrapper.vm.selectedFiles).toBeUndefined();
        expect(wrapper.vm.selectedAMPM).toBeUndefined();
        expect(wrapper.vm.search).toBeUndefined();
    });
});