import * as fs from "node:fs/promises";
import path from "node:path";
import { v4 as uuidv4 } from "uuid";

export class EFSService {
  private efsPath: string;

  constructor() {
    this.efsPath = process.env.EFS_MOUNT_PATH || "/mnt/efs";
  }

  public async saveFile(data: Buffer, fileName?: string): Promise<string> {
    try {
      await fs.mkdir(this.efsPath, { recursive: true });

      const name = fileName || `${uuidv4()}.pdf`;
      const filePath = path.join(this.efsPath, name);

      await fs.writeFile(filePath, data);

      console.log(`File saved to EFS: ${filePath}`);

      return filePath;
    } catch (error) {
      console.error("Error saving file to EFS:", {
        error: error instanceof Error ? error.message : String(error),
        fileName,
      });
      throw error;
    }
  }

  public async deleteFile(filePath: string): Promise<void> {
    try {
      await fs.unlink(filePath);
      console.log(`File deleted from EFS: ${filePath}`);
    } catch (error) {
      console.error("Error deleting file from EFS:", {
        error: error instanceof Error ? error.message : String(error),
        filePath,
      });
      throw error;
    }
  }
}
