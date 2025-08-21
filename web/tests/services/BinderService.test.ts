import { BinderService } from '@/services//BinderService';
import { IHttpService } from '@/services/HttpService';
import { Binder } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { beforeEach, describe, expect, it, Mock, vi } from 'vitest';

const mockHttpService = {
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn(),
} as unknown as IHttpService;

class TestableBinderService extends BinderService {
  constructor() {
    super(mockHttpService);
  }
}

describe('BinderService', () => {
  let service: BinderService;

  beforeEach(() => {
    vi.clearAllMocks();
    service = new TestableBinderService();
  });

  it('should fetch binders with query params', async () => {
    const queryParams = { page: 1 };
    const expectedResponse: ApiResponse<Binder[]> = {
      succeeded: true,
      errors: [],
      payload: [{} as Binder],
    };

    (mockHttpService.get as Mock).mockResolvedValueOnce(expectedResponse);

    const result = await service.getBinders(queryParams);
    expect(mockHttpService.get).toHaveBeenCalledWith(
      'api/binders',
      queryParams,
      {
        skipErrorHandler: true,
      }
    );
    expect(result).toEqual(expectedResponse);
  });

  it('should add a binder', async () => {
    const binder = {} as Binder;
    const expectedResponse: ApiResponse<Binder> = {
      succeeded: true,
      errors: [],
      payload: binder,
    };

    (mockHttpService.post as Mock).mockResolvedValueOnce(expectedResponse);

    const result = await service.addBinder(binder);
    expect(mockHttpService.post).toHaveBeenCalledWith(
      'api/binders',
      binder,
      {},
      'json',
      { skipErrorHandler: true }
    );
    expect(result).toEqual(expectedResponse);
  });

  it('should update a binder', async () => {
    const binder = {} as Binder;
    const expectedResponse: ApiResponse<Binder> = {
      succeeded: true,
      errors: [],
      payload: binder,
    };

    (mockHttpService.put as Mock).mockResolvedValueOnce(expectedResponse);

    const result = await service.updateBinder(binder);
    expect(mockHttpService.put).toHaveBeenCalledWith(
      'api/binders',
      binder,
      {},
      'json',
      { skipErrorHandler: true }
    );
    expect(result).toEqual(expectedResponse);
  });

  it('should delete a binder', async () => {
    const binderId = '1';

    (mockHttpService.delete as Mock).mockResolvedValueOnce(undefined);

    await service.deleteBinder(binderId);
    expect(mockHttpService.delete).toHaveBeenCalledWith(
      `api/binders/${binderId}`,
      {},
      'json',
      { skipErrorHandler: true }
    );
  });
});
