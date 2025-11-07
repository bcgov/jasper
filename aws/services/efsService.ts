import * as fs from "node:fs/promises";
import path from "node:path";
import { v4 as uuidv4 } from "uuid";
import { detectFileExtension } from "../util";

export class EFSService {
  private efsPath: string;

  constructor() {
    this.efsPath = process.env.EFS_MOUNT_PATH || "/mnt/efs";
  }

  async saveFile(data: Buffer): Promise<string> {
    const extension = detectFileExtension(data);
    const fileName = `${uuidv4()}${extension}`;
    try {
      const filePath = path.join(this.efsPath, fileName);

      await fs.mkdir(this.efsPath, { recursive: true });
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
}
