import * as qs from "qs";
import { describe, expect, it, vi } from "vitest";
import {
  detectFileExtension,
  replaceWithWildcard,
  sanitizeHeaders,
  sanitizeQueryStringParams,
} from "../util";

vi.mock("qs", () => ({
  stringify: vi.fn((params) => JSON.stringify(params)),
}));

describe("sanitizeHeaders", () => {
  it("should filter allowed headers", () => {
    const headers = {
      applicationCd: "app123",
      correlationId: "12345",
      unauthorizedHeader: "shouldBeRemoved",
      Accept: "application/octet-stream",
    };
    const result = sanitizeHeaders(headers);
    expect(result).toEqual({
      applicationCd: "app123",
      correlationId: "12345",
      Accept: "application/octet-stream",
    });
  });

  it("should return an empty object if no allowed headers are present", () => {
    const headers = { unauthorizedHeader: "shouldBeRemoved" };
    expect(sanitizeHeaders(headers)).toEqual({});
  });

  it("should return an empty object when Authorization header is present", () => {
    const headers = { Authorization: "Bearer 123" };
    expect(sanitizeHeaders(headers)).toEqual({});
  });
});

describe("sanitizeQueryStringParams", () => {
  it("should stringify and encode query params", () => {
    const params = { key1: "value1", key2: "value2" };
    const result = sanitizeQueryStringParams(params);
    expect(qs.stringify).toHaveBeenCalledWith(params, { encode: true });
    expect(result).toBe(JSON.stringify(params));
  });

  it("should handle JSON array strings properly", () => {
    const params = { key1: "[1,2,3]", key2: "notAnArray" };
    const result = sanitizeQueryStringParams(params);
    expect(result).toContain("[1,2,3]");
  });

  it("should log a warning if JSON parsing fails", () => {
    console.warn = vi.fn();
    const params = { key1: "['1','2','3',]", key2: "valid" };
    sanitizeQueryStringParams(params);
    expect(console.warn).toHaveBeenCalled();
  });
});

describe("replaceWithWildcard", () => {
  const testCases: [string, string][][] = [
    [
      [
        "arn:aws:execute-api:us-east-1:123456789012:abcd1234/prod/GET/users/123",
        "arn:aws:execute-api:us-east-1:123456789012:abcd1234/prod/*",
      ],
      [
        "arn:aws:execute-api:us-west-2:987654321098:wxyz5678/dev/POST/orders",
        "arn:aws:execute-api:us-west-2:987654321098:wxyz5678/dev/*",
      ],
    ],
    [
      [
        "arn:aws:execute-api:eu-central-1:111122223333:xyz123/test/DELETE/items/42",
        "arn:aws:execute-api:eu-central-1:111122223333:xyz123/test/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/stage/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/stage/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/other-stage/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/other-stage/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/dev-env/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/dev-env/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/some-other-stage/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/some-other-stage/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/some-other-stage-1/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/some-other-stage-1/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/lz-dev/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/lz-dev/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/staging_v2/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/staging_v2/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/test.env/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/test.env/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/prod-2024/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/prod-2024/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/dev_test-123.staging/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/dev_test-123.staging/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/v1/POST/resource/123",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/v1/*",
      ],
      [
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/PRODUCTION/GET",
        "arn:aws:execute-api:ap-southeast-1:444455556666:abcd1234/PRODUCTION/*",
      ],
    ],
  ];

  for (const row of testCases) {
    for (const [input, expected] of row) {
      it(`should convert '${input}' to '${expected}'`, () => {
        expect(replaceWithWildcard(input)).toBe(expected);
      });
    }
  }
});

describe("detectFileExtension", () => {
  it("should detect PDF files by magic bytes", () => {
    // PDF starts with %PDF (hex: 25 50 44 46)
    const pdfBuffer = Buffer.from([
      0x25, 0x50, 0x44, 0x46, 0x2d, 0x31, 0x2e, 0x34,
    ]);
    expect(detectFileExtension(pdfBuffer)).toBe(".pdf");
  });

  it("should detect PDF with minimal buffer", () => {
    const pdfBuffer = Buffer.from([0x25, 0x50, 0x44, 0x46]);
    expect(detectFileExtension(pdfBuffer)).toBe(".pdf");
  });

  it("should return .bin for non-PDF files", () => {
    // JPEG magic bytes (FF D8 FF)
    const jpegBuffer = Buffer.from([0xff, 0xd8, 0xff, 0xe0]);
    expect(detectFileExtension(jpegBuffer)).toBe(".bin");
  });

  it("should return .bin for PNG files", () => {
    // PNG magic bytes (89 50 4E 47)
    const pngBuffer = Buffer.from([
      0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
    ]);
    expect(detectFileExtension(pngBuffer)).toBe(".bin");
  });

  it("should return .bin for buffers smaller than 4 bytes", () => {
    const smallBuffer = Buffer.from([0x25, 0x50]);
    expect(detectFileExtension(smallBuffer)).toBe(".bin");
  });

  it("should return .bin for empty buffer", () => {
    const emptyBuffer = Buffer.from([]);
    expect(detectFileExtension(emptyBuffer)).toBe(".bin");
  });

  it("should return .bin for text files", () => {
    const textBuffer = Buffer.from("Hello World");
    expect(detectFileExtension(textBuffer)).toBe(".bin");
  });

  it("should return .bin for ZIP/DOCX files", () => {
    // ZIP magic bytes (50 4B 03 04)
    const zipBuffer = Buffer.from([0x50, 0x4b, 0x03, 0x04]);
    expect(detectFileExtension(zipBuffer)).toBe(".bin");
  });

  it("should handle PDF with different versions", () => {
    // PDF-1.7
    const pdf17Buffer = Buffer.from("%PDF-1.7\n", "ascii");
    expect(detectFileExtension(pdf17Buffer)).toBe(".pdf");

    // PDF-2.0
    const pdf20Buffer = Buffer.from("%PDF-2.0\n", "ascii");
    expect(detectFileExtension(pdf20Buffer)).toBe(".pdf");
  });
});
