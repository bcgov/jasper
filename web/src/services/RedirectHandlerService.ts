class RedirectHandler {
  handleUnauthorized(redirectUri: string) {
    const loginUrl = `${import.meta.env.BASE_URL}api/auth/login?redirectUri=${encodeURIComponent(redirectUri)}`;
    globalThis.location.replace(loginUrl);
  }
}

export default new RedirectHandler();
