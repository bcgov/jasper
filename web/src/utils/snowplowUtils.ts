/**
 * Call Snowplow refresh immediately (not tied to lifecycle)
 */
export function callRefreshLinkClickTracking() {
  globalThis.snowplow?.('refreshLinkClickTracking');
}

/**
 * Call Snowplow refresh immediately (not tied to lifecycle)
 */
export function callTrackPageView() {
  globalThis.snowplow?.('trackPageView');
}
