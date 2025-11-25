import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import QuickLinksMenu from 'CMP/shared/QuickLinksMenu.vue';
import type { QuickLink } from '@/types';
import { nextTick } from 'vue';

const mockQuickLinks: QuickLink[] = [
  {
    id: '1',
    name: 'Documents',
    parentName: '',
    isMenu: true,
    url: '',
    order: 1,
    judgeId: '',
  },
  {
    id: '2',
    name: 'Recent Files',
    parentName: 'Documents',
    isMenu: false,
    url: 'https://example.com/recent',
    order: 2,
    judgeId: '',
  },
  {
    id: '3',
    name: 'Archived Files',
    parentName: 'Documents',
    isMenu: false,
    url: 'https://example.com/archived',
    order: 3,
    judgeId: '',
  },
  {
    id: '4',
    name: 'Settings',
    parentName: '',
    isMenu: true,
    url: '',
    order: 4,
    judgeId: '',
  },
  {
    id: '5',
    name: 'Profile Settings',
    parentName: 'Settings',
    isMenu: false,
    url: 'https://example.com/profile',
    order: 5,
    judgeId: '',
  },
];

const mockQuickLinkService = {
  getQuickLinks: vi.fn(),
};

describe('QuickLinksMenu.vue', () => {
  let wrapper: any;

  beforeEach(() => {
    mockQuickLinkService.getQuickLinks.mockResolvedValue(mockQuickLinks);

    wrapper = mount(QuickLinksMenu, {
      global: {
        provide: {
          quickLinkService: mockQuickLinkService,
        },
      },
    });
  });

  it('renders the component', () => {
    expect(wrapper.exists()).toBe(true);
  });

  it('shows skeleton loader while loading', async () => {
    const loadingWrapper = mount(QuickLinksMenu, {
      global: {
        provide: {
          quickLinkService: {
            getQuickLinks: () => new Promise(() => {}),
          },
        },
      },
    });

    await nextTick();
    expect(loadingWrapper.find('v-skeleton-loader').exists()).toBe(true);
  });

  it('fetches quick links on mount', async () => {
    await nextTick();
    expect(mockQuickLinkService.getQuickLinks).toHaveBeenCalled();
  });

  it('transforms quick links into hierarchical menu structure', async () => {
    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 0));

    const menuItems = wrapper.vm.menuItems;
    expect(menuItems).toHaveLength(2);

    expect(menuItems[0].name).toBe('Documents');
    expect(menuItems[0].children).toHaveLength(2);
    expect(menuItems[0].children[0].name).toBe('Recent Files');
    expect(menuItems[0].children[1].name).toBe('Archived Files');

    expect(menuItems[1].name).toBe('Settings');
    expect(menuItems[1].children).toHaveLength(1);
    expect(menuItems[1].children[0].name).toBe('Profile Settings');
  });

  it('sorts parents and children by order', async () => {
    const unsortedLinks: QuickLink[] = [
      {
        id: '3',
        name: 'Parent C',
        parentName: '',
        isMenu: true,
        url: '',
        order: 30,
        judgeId: '',
      },
      {
        id: '1',
        name: 'Parent A',
        parentName: '',
        isMenu: true,
        url: '',
        order: 10,
        judgeId: '',
      },
      {
        id: '5',
        name: 'Child 2',
        parentName: 'Parent A',
        isMenu: false,
        url: '',
        order: 20,
        judgeId: '',
      },
      {
        id: '4',
        name: 'Child 1',
        parentName: 'Parent A',
        isMenu: false,
        url: '',
        order: 15,
        judgeId: '',
      },
    ];

    mockQuickLinkService.getQuickLinks.mockResolvedValue(unsortedLinks);

    const sortWrapper = mount(QuickLinksMenu, {
      global: {
        provide: {
          quickLinkService: mockQuickLinkService,
        },
      },
    });

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 0));

    const menuItems = sortWrapper.vm.menuItems as QuickLink[];
    expect(menuItems[0].name).toBe('Parent A');
    expect(menuItems[1].name).toBe('Parent C');
    expect(menuItems[0]?.children?.[0]?.name).toBe('Child 1');
    expect(menuItems[0]?.children?.[1]?.name).toBe('Child 2');
  });

  it('opens URL in new tab when child item is clicked', async () => {
    const openSpy = vi.spyOn(window, 'open').mockImplementation(() => null);

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 0));

    wrapper.vm.handleChildClick({
      id: '2',
      name: 'Recent Files',
      parentName: 'Documents',
      url: 'https://example.com/recent',
      order: 2,
      judgeId: '',
    });

    expect(openSpy).toHaveBeenCalledWith(
      'https://example.com/recent',
      '_blank'
    );

    openSpy.mockRestore();
  });

  it('does not open URL if child has no URL', async () => {
    const openSpy = vi.spyOn(window, 'open').mockImplementation(() => null);

    await nextTick();

    wrapper.vm.handleChildClick({
      id: '1',
      name: 'No URL Item',
      parentName: '',
      url: '',
      order: 1,
      judgeId: '',
    });

    expect(openSpy).not.toHaveBeenCalled();

    openSpy.mockRestore();
  });

  it('handles API errors gracefully', async () => {
    const consoleErrorSpy = vi
      .spyOn(console, 'error')
      .mockImplementation(() => {});
    const errorMessage = 'API Error';

    mockQuickLinkService.getQuickLinks.mockRejectedValue(
      new Error(errorMessage)
    );

    mount(QuickLinksMenu, {
      global: {
        provide: {
          quickLinkService: mockQuickLinkService,
        },
      },
    });

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(consoleErrorSpy).toHaveBeenCalledWith(
      'Error fetching quick links:',
      expect.any(Error)
    );

    consoleErrorSpy.mockRestore();
  });

  it('filters out menu items from children', async () => {
    const linksWithMenuChild: QuickLink[] = [
      {
        id: '1',
        name: 'Parent',
        parentName: '',
        isMenu: true,
        url: '',
        order: 1,
        judgeId: '',
      },
      {
        id: '2',
        name: 'Child Menu',
        parentName: 'Parent',
        isMenu: true,
        url: '',
        order: 2,
        judgeId: '',
      },
      {
        id: '3',
        name: 'Regular Child',
        parentName: 'Parent',
        isMenu: false,
        url: 'https://example.com',
        order: 3,
        judgeId: '',
      },
    ];

    mockQuickLinkService.getQuickLinks.mockResolvedValue(linksWithMenuChild);

    const filterWrapper = mount(QuickLinksMenu, {
      global: {
        provide: {
          quickLinkService: mockQuickLinkService,
        },
      },
    });

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 0));

    const menuItems = filterWrapper.vm.menuItems as QuickLink[];
    expect(menuItems[0]?.children).toHaveLength(1);
    expect(menuItems[0]?.children?.[0]?.name).toBe('Regular Child');
  });

  it('handles empty quick links array', async () => {
    mockQuickLinkService.getQuickLinks.mockResolvedValue([]);

    const emptyWrapper = mount(QuickLinksMenu, {
      global: {
        provide: {
          quickLinkService: mockQuickLinkService,
        },
      },
    });

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 0));

    const menuItems = emptyWrapper.vm.menuItems;
    expect(menuItems).toHaveLength(0);
  });
});
