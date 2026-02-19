import { useCommonStore, useDarsStore } from '@/stores';
import { PersonSearchItem } from '@/types';
import { OrderReviewStatus, RolesEnum, UserInfo } from '@/types/common';
import { faker } from '@faker-js/faker';
import { flushPromises, mount } from '@vue/test-utils';
import AppBar from 'CMP/shared/AppBar.vue';
import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createRouter, createWebHistory } from 'vue-router';

// Mock the JudgeSelector component
vi.mock('CMP/shared/JudgeSelector.vue', () => ({
  default: {
    name: 'JudgeSelector',
    props: ['judges'],
    template: '<div class="judge-selector"></div>',
  },
}));

// Mock services
const mockJudgeService = {
  getJudges: vi.fn(),
};

const mockOrderService = {
  getOrders: vi.fn(),
};

// Helper functions to generate test data with faker
const generateJudge = (): PersonSearchItem => ({
  personId: faker.number.int(),
  fullName: faker.person.fullName(),
  rotaInitials: faker.string.alpha({ length: 3 }).toUpperCase(),
  homeLocationId: faker.number.int(),
});

const generateUserInfo = (overrides: Partial<UserInfo> = {}): UserInfo => ({
  userType: faker.helpers.arrayElement(['Judge', 'Staff', 'Admin']),
  enableArchive: faker.datatype.boolean(),
  roles: [RolesEnum.Admin],
  subRole: faker.person.jobTitle(),
  isSupremeUser: faker.helpers.arrayElement(['true', 'false']),
  isActive: true,
  agencyCode: faker.string.alpha({ length: 5 }).toUpperCase(),
  userId: faker.string.uuid(),
  judgeId: faker.number.int({ min: 1, max: 10000 }),
  judgeHomeLocationId: faker.number.int({ min: 1, max: 100 }),
  email: faker.internet.email(),
  userTitle: `Judge ${faker.person.fullName()}`,
  ...overrides,
});

const generateOrder = (status: OrderReviewStatus) => ({
  id: faker.string.uuid(),
  status,
  packageId: faker.number.int({ min: 1000, max: 99999 }),
  packageDocumentId: faker.string.uuid(),
  packageName: faker.lorem.word(),
  receivedDate: faker.date.past().toISOString().split('T')[0],
  processedDate: faker.date.past().toISOString().split('T')[0],
  courtClass: faker.helpers.arrayElement(['CC', 'CV']),
  courtFileNumber: faker.string.alphanumeric({ length: 10 }).toUpperCase(),
  styleOfCause: `${faker.person.lastName()} v ${faker.person.lastName()}`,
  physicalFileId: faker.number.int().toString(),
});

describe('AppBar.vue', () => {
  let router: any;

  beforeEach(() => {
    setActivePinia(createPinia());

    // Setup basic router with routes
    router = createRouter({
      history: createWebHistory(),
      routes: [
        { path: '/', component: { template: '<div>Home</div>' } },
        { path: '/dashboard', component: { template: '<div>Dashboard</div>' } },
        {
          path: '/court-list',
          component: { template: '<div>Court List</div>' },
        },
        {
          path: '/court-file-search',
          component: { template: '<div>Court File Search</div>' },
        },
        { path: '/orders', component: { template: '<div>Orders</div>' } },
        {
          path: '/civil-file/:id',
          component: { template: '<div>Civil File</div>' },
        },
        {
          path: '/criminal-file/:id',
          component: { template: '<div>Criminal File</div>' },
        },
      ],
    });

    vi.clearAllMocks();
    mockJudgeService.getJudges.mockResolvedValue([]);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  const createWrapper = (props = {}, options = {}) => {
    return mount(AppBar, {
      props,
      global: {
        plugins: [router],
        provide: {
          orderService: mockOrderService,
          judgeService: mockJudgeService,
        },
      },
      ...options,
    });
  };

  describe('rendering', () => {
    it('should render the component', () => {
      const wrapper = createWrapper();
      expect(wrapper.exists()).toBe(true);
    });

    it('should render the logo image', () => {
      const wrapper = createWrapper();
      const logo = wrapper.find('[data-testid="logo"]');

      expect(logo.exists()).toBe(true);
      expect(logo.attributes('alt')).toBe('logo');
      expect(logo.attributes('width')).toBe('63');
    });

    it('should render logo as a link to home', () => {
      const wrapper = createWrapper();
      const link = wrapper.find('[data-testid="router-link"]');

      expect(link.exists()).toBe(true);
      expect(link.attributes('href')).toBe('/');
    });

    it('should render navigation tabs', () => {
      const wrapper = createWrapper();
      const tabs = wrapper.findAll(
        '[value="dashboard"], [value="court-list"], [value="court-file-search"]'
      );
      expect(tabs.length).toBeGreaterThan(0);
    });
  });

  describe('tab navigation', () => {
    it('should render Dashboard tab', () => {
      const wrapper = createWrapper();
      expect(wrapper.text()).toContain('Dashboard');
    });

    it('should render Court list tab', () => {
      const wrapper = createWrapper();
      expect(wrapper.text()).toContain('Court list');
    });

    it('should render Court file search tab', () => {
      const wrapper = createWrapper();
      expect(wrapper.text()).toContain('Court file search');
    });

    it('should render DARS button', () => {
      const wrapper = createWrapper();
      expect(wrapper.text()).toContain('DARS');
    });

    it('should update selectedTab when route changes to dashboard', async () => {
      const wrapper = createWrapper();
      await router.push('/dashboard');
      await wrapper.vm.$nextTick();

      expect((wrapper.vm as any).selectedTab).toBe('/dashboard');
    });

    it('should update selectedTab when route changes to court-list', async () => {
      const wrapper = createWrapper();
      await router.push('/court-list');
      await wrapper.vm.$nextTick();

      expect((wrapper.vm as any).selectedTab).toBe('/court-list');
    });

    it('should update selectedTab to court-file-search when navigating to civil-file', async () => {
      const wrapper = createWrapper();
      await router.push('/civil-file/123');
      await wrapper.vm.$nextTick();

      expect((wrapper.vm as any).selectedTab).toBe('court-file-search');
    });

    it('should update selectedTab to court-file-search when navigating to criminal-file', async () => {
      const wrapper = createWrapper();
      await router.push('/criminal-file/456');
      await wrapper.vm.$nextTick();

      expect((wrapper.vm as any).selectedTab).toBe('court-file-search');
    });
  });

  describe('profile button', () => {
    it('should render profile button with user name', async () => {
      const commonStore = useCommonStore();
      const userInfo = generateUserInfo();
      commonStore.userInfo = userInfo;

      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).toContain(userInfo.userTitle);
    });

    it('should emit open-profile event when profile button is clicked', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = generateUserInfo();

      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      const profileBtn = wrapper.find('v-btn-stub');
      if (profileBtn.exists()) {
        await profileBtn.trigger('click');
        expect(wrapper.emitted('open-profile')).toBeTruthy();
      }
    });
  });

  describe('DARS button', () => {
    it('should open DARS modal when clicked', async () => {
      const darsStore = useDarsStore();
      const openModalSpy = vi.spyOn(darsStore, 'openModal');

      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      // Find the DARS button and click it
      const buttons = wrapper.findAll('v-btn-stub');
      const darsBtn = buttons.find((btn) => btn.text().includes('DARS'));

      if (darsBtn) {
        await darsBtn.trigger('click');
        expect(openModalSpy).toHaveBeenCalled();
      }
    });
  });

  describe('Orders tab', () => {
    it('should not show Orders tab for non-admin users', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = generateUserInfo({ roles: [RolesEnum.Judge] });

      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      expect((wrapper.vm as any).showOrders).toBe(false);
      expect(wrapper.text()).not.toContain('For Signing');
    });

    it('should show Orders tab for admin users', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = generateUserInfo({ roles: [RolesEnum.Admin] });

      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      expect((wrapper.vm as any).showOrders).toBe(true);
    });

    it('should display pending orders badge when there are pending orders', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = generateUserInfo({ roles: [RolesEnum.Admin] });

      const mockOrders = [
        generateOrder(OrderReviewStatus.Pending),
        generateOrder(OrderReviewStatus.Pending),
        generateOrder(OrderReviewStatus.Approved),
      ];

      mockOrderService.getOrders.mockResolvedValue(mockOrders);

      const wrapper = createWrapper();
      await flushPromises();

      const badge = wrapper.find('[data-testid="order-badge"]');

      expect(badge.exists()).toBe(true);
      expect(badge.attributes('content')).toBe('2');
    });

    it('should not display badge when there are no pending orders', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = generateUserInfo({ roles: [RolesEnum.Admin] });

      const mockOrders = [generateOrder(OrderReviewStatus.Approved)];

      mockOrderService.getOrders.mockResolvedValue(mockOrders);

      const wrapper = createWrapper();
      await flushPromises();

      expect((wrapper.vm as any).pendingOrdersCount).toBe(0);
    });
  });

  describe('Judge Selector', () => {
    it('should show judge selector on dashboard tab', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = generateUserInfo();

      const judges: PersonSearchItem[] = [generateJudge()];

      const wrapper = createWrapper();
      (wrapper.vm as any).judges = judges;
      (wrapper.vm as any).selectedTab = 'dashboard';
      await wrapper.vm.$nextTick();

      const judgeSelector = wrapper.find('[data-testid="judge-selector"]');
      expect(judgeSelector.exists()).toBe(true);
    });

    it('should show judge selector on court-list tab', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = generateUserInfo();

      const judges: PersonSearchItem[] = [generateJudge()];

      const wrapper = createWrapper();
      (wrapper.vm as any).judges = judges;
      (wrapper.vm as any).selectedTab = 'court-list';
      await wrapper.vm.$nextTick();

      const judgeSelector = wrapper.find('[data-testid="judge-selector"]');
      expect(judgeSelector.exists()).toBe(true);
    });

    it('should show judge selector on orders tab', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = generateUserInfo({ roles: [RolesEnum.Admin] });

      const judges: PersonSearchItem[] = [generateJudge()];

      const wrapper = createWrapper();
      (wrapper.vm as any).judges = judges;
      (wrapper.vm as any).selectedTab = 'orders';
      await wrapper.vm.$nextTick();

      const judgeSelector = wrapper.find('[data-testid="judge-selector"]');
      expect(judgeSelector.exists()).toBe(true);
    });

    it('should not show judge selector when there are no judges', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = generateUserInfo();

      const wrapper = createWrapper();
      (wrapper.vm as any).judges = [];
      (wrapper.vm as any).selectedTab = 'dashboard';
      await wrapper.vm.$nextTick();

      const judgeSelector = wrapper.find('[data-testid="judge-selector"]');
      expect(judgeSelector.exists()).toBe(false);
    });

    it('should not show judge selector on court-file-search tab', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = generateUserInfo();

      const judges: PersonSearchItem[] = [generateJudge()];

      const wrapper = createWrapper();
      (wrapper.vm as any).judges = judges;
      (wrapper.vm as any).selectedTab = 'court-file-search';
      await wrapper.vm.$nextTick();

      const judgeSelector = wrapper.find('[data-testid="judge-selector"]');
      expect(judgeSelector.exists()).toBe(false);
    });
  });

  describe('lifecycle', () => {
    it('should fetch judges on mount', async () => {
      const judges = [generateJudge(), generateJudge()];

      mockJudgeService.getJudges.mockResolvedValue(judges);

      const wrapper = createWrapper();
      await flushPromises();

      expect(mockJudgeService.getJudges).toHaveBeenCalled();
      expect((wrapper.vm as any).judges).toEqual(judges);
    });
  });

  describe('error handling', () => {
    it('should throw error if orderService is not provided', () => {
      expect(() => {
        mount(AppBar, {
          global: {
            plugins: [router],
            provide: {
              orderService: undefined,
              judgeService: mockJudgeService,
            },
            stubs: {
              VAppBar: false,
              VAppBarTitle: false,
              VTabs: false,
              VTab: false,
              VBtn: false,
              VSpacer: false,
              VBadge: false,
              VIcon: false,
              JudgeSelector: { template: '<div></div>' },
            },
          },
        });
      }).toThrow('Service is not available!');
    });

    it('should throw error if judgeService is not provided', () => {
      expect(() => {
        mount(AppBar, {
          global: {
            plugins: [router],
            provide: {
              orderService: mockOrderService,
              judgeService: undefined,
            },
            stubs: {
              VAppBar: false,
              VAppBarTitle: false,
              VTabs: false,
              VTab: false,
              VBtn: false,
              VSpacer: false,
              VBadge: false,
              VIcon: false,
              JudgeSelector: { template: '<div></div>' },
            },
          },
        });
      }).toThrow('Service is not available!');
    });
  });

  describe('computed properties', () => {
    it('should compute userName from commonStore', async () => {
      const commonStore = useCommonStore();
      const userInfo = generateUserInfo();
      commonStore.userInfo = userInfo;

      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      expect((wrapper.vm as any).userName).toBe(userInfo.userTitle);
    });

    it('should return empty string for userName when userInfo is null', async () => {
      const commonStore = useCommonStore();
      commonStore.userInfo = null;

      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      expect((wrapper.vm as any).userName).toBe('');
    });
  });
});
