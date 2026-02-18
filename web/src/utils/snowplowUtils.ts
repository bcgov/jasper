/**
 * Call Snowplow refresh immediately (not tied to lifecycle)
 */
export function callRefreshLinkClickTracking() {
  window.snowplow?.('refreshLinkClickTracking');
};

/**
 * Call Snowplow refresh immediately (not tied to lifecycle)
 */
export function callTrackPageView() {
  window.snowplow?.('trackPageView');
};