import qs = require("qs");
import { describe, expect, it, vi } from "vitest";
import { sanitizeHeaders, sanitizeQueryStringParams } from "../util";

vi.spyOn(qs, "stringify").mockImplementation((params) =>
  JSON.stringify(params)
);

describe("sanitizeHeaders", () => {
  it("should filter allowed headers", () => {
    const headers = {
      applicationCd: "app123",
      correlationId: "12345",
      unauthorizedHeader: "shouldBeRemoved",
    };
    const result = sanitizeHeaders(headers);
    expect(result).toEqual({
      applicationCd: "app123",
      correlationId: "12345",
    });
  });

  it("should return an empty object if no allowed headers are present", () => {
    const headers = { unauthorizedHeader: "shouldBeRemoved" };
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
