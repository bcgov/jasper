import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import CourtListCard from 'CMP/courtlist/CourtListCard.vue';
import { CourtListCardInfo } from '@/types/courtlist';

const createWrapper = () => {
    const card: CourtListCardInfo = {
        courtListLocation: 'Court A',
        courtListLocationID: 1,
        courtListRoom: 'Room 101',
        activity: 'Hearing',
        amPM: 'AM',
        fileCount: 5,
        presider: 'Judge Smith',
        courtClerk: 'John Doe',
        email: 'court@example.com'
    };
    return mount(CourtListCard, {
        props: {
            cardInfo: card
        },
    });
};

describe('CourtListCard.vue', () => {
    it('renders all court list details correctly', () => {
        const wrapper = createWrapper();
        expect(wrapper.text()).toContain('Court A');
        expect(wrapper.text()).toContain('Rooms: Room 101 (AM)');
        expect(wrapper.text()).toContain('Presider: Judge Smith');
        expect(wrapper.text()).toContain('Court clerk: John Doe');
        expect(wrapper.text()).toContain('Activity: Hearing');
        expect(wrapper.text()).toContain('Scheduled: 5 files');
        expect(wrapper.text()).toContain('court@example.com');
    });
});
