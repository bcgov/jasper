import * as qs from "qs";

// These are the list of Headers imported from SCV
// Only include headers from the original request when present.
const allowedHeaders = new Set([
  "Accept",
  "applicationCd",
  "correlationId",
  "deviceNm",
  "domainNm",
  "domainUserGuid",
  "domainUserId",
  "guid",
  "ipAddressTxt",
  "reloadPassword",
  "requestAgencyIdentifierId",
  "requestPartId",
  "temporaryAccessGuid",
]);

/**
 * Sanitizes headers by removing any headers not in the allowed list.
 * @param headers - The headers to sanitize.
 * @returns A sanitized headers object.
 */
export const sanitizeHeaders = (
  headers: Record<string, string | undefined>
): Record<string, string> => {
  const filteredHeaders: Record<string, string> = {};

  for (const [key, value] of Object.entries(headers || {})) {
    if (allowedHeaders.has(key) && value !== undefined) {
      filteredHeaders[key] = value;
    }
  }

  return filteredHeaders;
};

/**
 * Sanitizes query string parameters by removing any parameters with undefined values.
 * @param params - The query string parameters to sanitize.
 * @returns A sanitized query string.
 */
export const sanitizeQueryStringParams = (
  params: Record<string, unknown>
): string => {
  for (const key of Object.keys(params)) {
    const value = params[key];

    // Check if the value JSON array
    if (
      typeof value === "string" &&
      value.startsWith("[") &&
      value.endsWith("]")
    ) {
      try {
        const parsedValue = JSON.parse(value);

        if (Array.isArray(parsedValue)) {
          params[key] = JSON.stringify(parsedValue);
        }
      } catch (error) {
        console.warn(`Failed to parse ${key}: ${value}`, error);
      }
    }
  }

  const queryString = qs.stringify(params, { encode: true });

  console.log(`Sanitized encoded qs: ${queryString}`);

  return queryString;
};

/**
 * Replaces everything after the API Gateway stage in the ARN with a wildcard (`/*`)
 * to take advantage of authorizer's caching capability
 * @param value - The ARN value to modify
 * @returns The modified ARN with a wildcard
 */
export const replaceWithWildcard = (value: string): string => {
  return value.replace(/^([^/]+\/[^/]+)(?:\/.*)?$/, "$1/*");
};

/**
 * Detects file extension from buffer using magic bytes (file signature)
 * Currently supports PDF detection only
 *
 * @param buffer - File buffer to analyze
 * @returns ".pdf" if PDF detected, otherwise ".bin"
 */
export const detectFileExtension = (buffer: Buffer): string => {
  if (buffer.length < 4) return ".bin";

  const magic = buffer.subarray(0, 4).toString("hex").toUpperCase();

  // PDF: starts with %PDF (hex: 25 50 44 46)
  if (magic.startsWith("25504446")) return ".pdf";

  return ".bin";
};
