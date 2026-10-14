export {};

declare global {
  interface Window {
    // Snowplow analytics tracker
    snowplow?: (command: string, ...args: any[]) => void;
  }
}
