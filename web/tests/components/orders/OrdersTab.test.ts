import { mount } from '@vue/test-utils';
import OrdersTab from 'CMP/orders/OrdersTab.vue';
import { describe, expect, it } from 'vitest';
import { createRouter, createWebHistory } from 'vue-router';

const createTestRouter = () =>
  createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/', component: { template: '<div>Home</div>' } },
      { path: '/orders', component: { template: '<div>Orders</div>' } },
      {
        path: '/desk-orders',
        component: { template: '<div>Desk Orders</div>' },
      },
    ],
  });

const defaultProps = {
  value: 'orders',
  title: 'For Signing',
  label: 'orders',
  priorityCount: 0,
  regularCount: 0,
  pulseActive: false,
};

const mountTab = (props: Partial<typeof defaultProps> = {}) =>
  mount(OrdersTab, {
    props: { ...defaultProps, ...props },
    global: {
      plugins: [createTestRouter()],
    },
  });

describe('OrdersTab.vue', () => {
  describe('rendering', () => {
    it('renders the component', () => {
      const wrapper = mountTab();
      expect(wrapper.exists()).toBe(true);
    });

    it('renders the title text', () => {
      const wrapper = mountTab({ title: 'For Signing' });
      expect(wrapper.text()).toContain('For Signing');
    });

    it('renders the Applications title when provided', () => {
      const wrapper = mountTab({
        value: 'desk-orders',
        title: 'Applications',
        label: 'desk orders',
      });
      expect(wrapper.text()).toContain('Applications');
    });
  });

  describe('badges - empty state', () => {
    it('does not render any badge when both counts are zero', () => {
      const wrapper = mountTab({ priorityCount: 0, regularCount: 0 });
      expect(wrapper.find('[data-testid="priority-badge"]').exists()).toBe(
        false
      );
      expect(wrapper.find('[data-testid="regular-badge"]').exists()).toBe(
        false
      );
      expect(wrapper.find('[data-testid="order-combo-badge"]').exists()).toBe(
        false
      );
    });

    it('renders only the title when both counts are zero', () => {
      const wrapper = mountTab({
        title: 'For Signing',
        priorityCount: 0,
        regularCount: 0,
      });
      expect(wrapper.text().trim()).toBe('For Signing');
    });
  });

  describe('badges - priority only', () => {
    it('renders the priority badge when priorityCount > 0 and regularCount === 0', () => {
      const wrapper = mountTab({ priorityCount: 3, regularCount: 0 });
      const badge = wrapper.find('[data-testid="priority-badge"]');
      expect(badge.exists()).toBe(true);
    });

    it('sets the badge content to the priority count', () => {
      const wrapper = mountTab({ priorityCount: 5, regularCount: 0 });
      const badge = wrapper.find('[data-testid="priority-badge"]');
      expect(badge.attributes('content')).toBe('5');
    });

    it('sets a descriptive title attribute on the priority badge', () => {
      const wrapper = mountTab({
        priorityCount: 2,
        regularCount: 0,
        label: 'orders',
      });
      const badge = wrapper.find('[data-testid="priority-badge"]');
      expect(badge.attributes('title')).toBe('2 priority orders pending');
    });

    it('uses the provided label in the priority badge title', () => {
      const wrapper = mountTab({
        priorityCount: 1,
        regularCount: 0,
        label: 'desk orders',
      });
      const badge = wrapper.find('[data-testid="priority-badge"]');
      expect(badge.attributes('title')).toBe('1 priority desk orders pending');
    });

    it('does not render regular or combo badges when only priority is set', () => {
      const wrapper = mountTab({ priorityCount: 3, regularCount: 0 });
      expect(wrapper.find('[data-testid="regular-badge"]').exists()).toBe(
        false
      );
      expect(wrapper.find('[data-testid="order-combo-badge"]').exists()).toBe(
        false
      );
    });
  });

  describe('badges - regular only', () => {
    it('renders the regular badge when regularCount > 0 and priorityCount === 0', () => {
      const wrapper = mountTab({ priorityCount: 0, regularCount: 4 });
      const badge = wrapper.find('[data-testid="regular-badge"]');
      expect(badge.exists()).toBe(true);
    });

    it('sets the badge content to the regular count', () => {
      const wrapper = mountTab({ priorityCount: 0, regularCount: 7 });
      const badge = wrapper.find('[data-testid="regular-badge"]');
      expect(badge.attributes('content')).toBe('7');
    });

    it('sets a descriptive title attribute on the regular badge', () => {
      const wrapper = mountTab({
        priorityCount: 0,
        regularCount: 4,
        label: 'orders',
      });
      const badge = wrapper.find('[data-testid="regular-badge"]');
      expect(badge.attributes('title')).toBe('4 regular orders pending');
    });

    it('uses the provided label in the regular badge title', () => {
      const wrapper = mountTab({
        priorityCount: 0,
        regularCount: 2,
        label: 'desk orders',
      });
      const badge = wrapper.find('[data-testid="regular-badge"]');
      expect(badge.attributes('title')).toBe('2 regular desk orders pending');
    });

    it('does not render priority or combo badges when only regular is set', () => {
      const wrapper = mountTab({ priorityCount: 0, regularCount: 4 });
      expect(wrapper.find('[data-testid="priority-badge"]').exists()).toBe(
        false
      );
      expect(wrapper.find('[data-testid="order-combo-badge"]').exists()).toBe(
        false
      );
    });
  });

  describe('badges - combo (priority + regular)', () => {
    it('renders the combo badge when both counts > 0', () => {
      const wrapper = mountTab({ priorityCount: 2, regularCount: 3 });
      const badge = wrapper.find('[data-testid="order-combo-badge"]');
      expect(badge.exists()).toBe(true);
    });

    it('does not render single priority or regular badges when combo is shown', () => {
      const wrapper = mountTab({ priorityCount: 2, regularCount: 3 });
      expect(wrapper.find('[data-testid="priority-badge"]').exists()).toBe(
        false
      );
      expect(wrapper.find('[data-testid="regular-badge"]').exists()).toBe(
        false
      );
    });

    it('still renders the title text alongside the combo badge', () => {
      const wrapper = mountTab({
        title: 'For Signing',
        priorityCount: 2,
        regularCount: 3,
      });
      expect(wrapper.text()).toContain('For Signing');
    });
  });

  describe('pulse animation', () => {
    it('applies the badge-pulse class when pulseActive is true (priority only)', () => {
      const wrapper = mountTab({
        priorityCount: 1,
        regularCount: 0,
        pulseActive: true,
      });
      const badge = wrapper.find('[data-testid="priority-badge"]');
      expect(badge.classes()).toContain('badge-pulse');
    });

    it('does not apply badge-pulse class when pulseActive is false (priority only)', () => {
      const wrapper = mountTab({
        priorityCount: 1,
        regularCount: 0,
        pulseActive: false,
      });
      const badge = wrapper.find('[data-testid="priority-badge"]');
      expect(badge.classes()).not.toContain('badge-pulse');
    });

    it('applies the badge-pulse class when pulseActive is true (regular only)', () => {
      const wrapper = mountTab({
        priorityCount: 0,
        regularCount: 1,
        pulseActive: true,
      });
      const badge = wrapper.find('[data-testid="regular-badge"]');
      expect(badge.classes()).toContain('badge-pulse');
    });

    it('does not apply badge-pulse class when pulseActive is false (regular only)', () => {
      const wrapper = mountTab({
        priorityCount: 0,
        regularCount: 1,
        pulseActive: false,
      });
      const badge = wrapper.find('[data-testid="regular-badge"]');
      expect(badge.classes()).not.toContain('badge-pulse');
    });

    it('applies the badge-pulse class when pulseActive is true (combo)', () => {
      const wrapper = mountTab({
        priorityCount: 1,
        regularCount: 1,
        pulseActive: true,
      });
      const badge = wrapper.find('[data-testid="order-combo-badge"]');
      expect(badge.classes()).toContain('badge-pulse');
    });

    it('does not apply badge-pulse class when pulseActive is false (combo)', () => {
      const wrapper = mountTab({
        priorityCount: 1,
        regularCount: 1,
        pulseActive: false,
      });
      const badge = wrapper.find('[data-testid="order-combo-badge"]');
      expect(badge.classes()).not.toContain('badge-pulse');
    });
  });
});
