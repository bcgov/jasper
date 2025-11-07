import * as fs from "node:fs/promises";
import path from "node:path";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { EFSService } from "../../services/efsService";

vi.mock("node:fs/promises", () => {
  return {
    mkdir: vi.fn(),
    writeFile: vi.fn(),
  };
});

vi.mock("uuid", () => ({
  v4: vi.fn(() => "test-uuid-1234"),
}));

vi.mock("../../util", () => ({
  detectFileExtension: vi.fn(() => ".pdf"),
}));

describe("EFSService", () => {
  let efsService: EFSService;
  const mockEfsPath = "/mnt/efs";

  beforeEach(() => {
    vi.clearAllMocks();
    // @ts-expect-error - Mocked function
    fs.mkdir.mockResolvedValue(undefined);
    // @ts-expect-error - Mocked function
    fs.writeFile.mockResolvedValue(undefined);
    process.env.EFS_MOUNT_PATH = mockEfsPath;
    efsService = new EFSService();
  });

  describe("saveFile", () => {
    it("should save file with generated UUID filename", async () => {
      const mockBuffer = Buffer.from("test file content");
      const expectedFilePath = path.join(mockEfsPath, "test-uuid-1234.pdf");

      const result = await efsService.saveFile(mockBuffer);

      expect(fs.mkdir).toHaveBeenCalledWith(mockEfsPath, { recursive: true });
      expect(fs.writeFile).toHaveBeenCalledWith(expectedFilePath, mockBuffer);
      expect(result).toBe(expectedFilePath);
    });

    it("should create EFS directory if it doesn't exist", async () => {
      const mockBuffer = Buffer.from("test content");
      await efsService.saveFile(mockBuffer);
      expect(fs.mkdir).toHaveBeenCalledWith(mockEfsPath, { recursive: true });
    });

    it("should use default EFS path if env not set", () => {
      delete process.env.EFS_MOUNT_PATH;
      const service = new EFSService();
      expect(service["efsPath"]).toBe("/mnt/efs");
    });

    it("should throw when mkdir fails", async () => {
      const mockError = new Error("Permission denied");
      // @ts-expect-error - Mocked function
      fs.mkdir.mockRejectedValue(mockError);
      await expect(efsService.saveFile(Buffer.from("x"))).rejects.toThrow(
        "Permission denied"
      );
    });

    it("should throw when writeFile fails", async () => {
      const mockError = new Error("Disk full");
      // @ts-expect-error - Mocked function
      fs.writeFile.mockRejectedValue(mockError);
      await expect(efsService.saveFile(Buffer.from("x"))).rejects.toThrow(
        "Disk full"
      );
    });

    it("should handle empty buffer", async () => {
      const mockBuffer = Buffer.from("");
      const expectedFilePath = path.join(mockEfsPath, "test-uuid-1234.pdf");

      const result = await efsService.saveFile(mockBuffer);

      expect(fs.writeFile).toHaveBeenCalledWith(expectedFilePath, mockBuffer);
      expect(result).toBe(expectedFilePath);
    });

    it("should generate correct file path with custom EFS mount path", async () => {
      const customPath = "/custom/efs/path";
      process.env.EFS_MOUNT_PATH = customPath;
      const customService = new EFSService();
      const mockBuffer = Buffer.from("test content");
      const expectedFilePath = path.join(customPath, "test-uuid-1234.pdf");

      const result = await customService.saveFile(mockBuffer);

      expect(fs.mkdir).toHaveBeenCalledWith(customPath, { recursive: true });
      expect(result).toBe(expectedFilePath);
    });
  });
});
