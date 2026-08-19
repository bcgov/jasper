import { HttpService } from '@/services/HttpService';
import { UserService } from '@/services/UserService';
import { UserInfo } from '@/types/common';
import { beforeEach, describe, expect, it, Mock, vi } from 'vitest';

const mockHttpService = {
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn(),
} as unknown as HttpService;

class TestableUserService extends UserService {
  constructor() {
    super(mockHttpService);
  }
}

describe('UserService', () => {
  let service: UserService;

  beforeEach(() => {
    vi.clearAllMocks();
    service = new TestableUserService();
  });

  describe('requestAccess', () => {
    const mockUser = { userId: '1' } as unknown as UserInfo;

    it('should call the correct URL and skip the error handler', async () => {
      (mockHttpService.put as Mock).mockResolvedValueOnce(mockUser);

      const result = await service.requestAccess();

      expect(mockHttpService.put).toHaveBeenCalledWith(
        'api/users/request-access',
        { skipErrorHandler: true }
      );
      expect(result).toEqual(mockUser);
    });

    it('should handle API errors', async () => {
      const error = new Error('Network error');
      (mockHttpService.put as Mock).mockRejectedValueOnce(error);

      await expect(service.requestAccess()).rejects.toThrow('Network error');
    });
  });

  describe('getMyUser', () => {
    const mockUser = { userId: '1' } as unknown as UserInfo;

    it('should call the correct URL', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockUser);

      const result = await service.getMyUser();

      expect(mockHttpService.get).toHaveBeenCalledWith('api/users/me');
      expect(result).toEqual(mockUser);
    });

    it('should handle API errors', async () => {
      const error = new Error('Network error');
      (mockHttpService.get as Mock).mockRejectedValueOnce(error);

      await expect(service.getMyUser()).rejects.toThrow('Network error');
    });
  });

  describe('markReleaseNotesViewed', () => {
    it('should call the correct URL with the version', async () => {
      (mockHttpService.post as Mock).mockResolvedValueOnce(undefined);

      await service.markReleaseNotesViewed('1.2.3');

      expect(mockHttpService.post).toHaveBeenCalledWith(
        'api/users/me/release-notes',
        { version: '1.2.3' }
      );
    });

    it('should handle API errors', async () => {
      const error = new Error('Network error');
      (mockHttpService.post as Mock).mockRejectedValueOnce(error);

      await expect(service.markReleaseNotesViewed('1.2.3')).rejects.toThrow(
        'Network error'
      );
    });
  });

  describe('getSignature', () => {
    const mockBlob = new Blob(['signature'], { type: 'image/png' });

    it('should request the signature as a blob', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockBlob);

      const result = await service.getSignature();

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/users/me/signature',
        {},
        { responseType: 'blob' }
      );
      expect(result).toBe(mockBlob);
    });

    it('should handle API errors', async () => {
      const error = new Error('Network error');
      (mockHttpService.get as Mock).mockRejectedValueOnce(error);

      await expect(service.getSignature()).rejects.toThrow('Network error');
    });
  });

  describe('getInitials', () => {
    const mockBlob = new Blob(['initials'], { type: 'image/png' });

    it('should request the initials as a blob', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockBlob);

      const result = await service.getInitials();

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/users/me/initials',
        {},
        { responseType: 'blob' }
      );
      expect(result).toBe(mockBlob);
    });

    it('should handle API errors', async () => {
      const error = new Error('Network error');
      (mockHttpService.get as Mock).mockRejectedValueOnce(error);

      await expect(service.getInitials()).rejects.toThrow('Network error');
    });
  });
});
