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

export const sanitizeQueryStringParams = (
  params: Record<string, unknown>
): string => {
  Object.keys(params).forEach((key) => {
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
  });

  const queryString = qs.stringify(params, { encode: true });

  console.log(`Sanitized encoded qs: ${queryString}`);

  return queryString;
};

export const replaceWithWildcard = (value: string): string => {
  // Replaces everything after the API Gateway stage in the ARN with a wildcard (`/*`)
  // to take advantage of authorizer's caching capability
  return value.replace(/^([^/]+\/[^/]+)(?:\/.*)?$/, "$1/*");
};
