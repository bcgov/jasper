const authErrorParam = 'authError';
const failedSignIn = 'authentication_failed';
const attemptKey = 'jasper.signInAttempt';
const attemptLifetimeMs = 5 * 60 * 1000;

interface SignInAttempt {
  returnUrl: string;
  step: 'login' | 'logout' | 'login-after-logout';
  expiresAt: number;
}

class RedirectHandler {
  private pageState: 'idle' | 'completed' | { redirectUntil: number } = 'idle';

  get isRedirecting(): boolean {
    return (
      typeof this.pageState === 'object' &&
      this.pageState.redirectUntil > Date.now()
    );
  }

  hasAuthenticationFailure(url = globalThis.location.href): boolean {
    const errors = new URL(url).searchParams.getAll(authErrorParam);
    return (
      errors.includes(failedSignIn) ||
      errors.includes('pcss_authorization_failed')
    );
  }

  // True also covers a redirect already started by another request on this page.
  trySessionRedirect(status: 401 | 409): boolean {
    if (this.isRedirecting) return true;
    if (this.hasAuthenticationFailure()) return false;

    const destination = this.cleanReturnUrl(new URL(globalThis.location.href));
    const previous = this.readAttempt(destination);
    if (
      previous &&
      (status === 401 ? previous.step !== 'logout' : previous.step !== 'login')
    ) {
      return false;
    }

    try {
      this.startRedirect({
        returnUrl: previous?.returnUrl ?? destination.href,
        step:
          status === 409 ? 'logout' : previous ? 'login-after-logout' : 'login',
        expiresAt: previous?.expiresAt ?? Date.now() + attemptLifetimeMs,
      });
      return true;
    } catch {
      return false;
    }
  }

  retrySignIn() {
    if (this.isRedirecting) return;

    const current = new URL(globalThis.location.href);
    const fromFailure = this.hasAuthenticationFailure(current.href);
    const destination = this.cleanReturnUrl(current);
    const previous = this.readAttempt(destination, fromFailure);
    this.startRedirect({
      returnUrl: previous?.returnUrl ?? destination.href,
      step: 'login',
      expiresAt: Date.now() + attemptLifetimeMs,
    });
  }

  markInitializationFailed() {
    if (this.hasAuthenticationFailure()) return;

    const url = new URL(globalThis.location.href);
    url.searchParams.set(authErrorParam, failedSignIn);
    try {
      globalThis.history.replaceState(globalThis.history.state, '', url);
    } catch {
      // The failure screen is already visible if the browser blocks the URL update.
    }
  }

  clearAttempt() {
    if (this.isRedirecting) return;
    this.pageState = 'completed';
    try {
      globalThis.sessionStorage.removeItem(attemptKey);
    } catch {
      // Some storage implementations can still overwrite a completed attempt.
      this.saveAttempt(null);
    }
  }

  private get baseUrl(): URL {
    const base = new URL(globalThis.location.origin);
    const { pathname } = new URL(import.meta.env.BASE_URL, base);
    base.pathname = pathname.endsWith('/') ? pathname : pathname + '/';
    return base;
  }

  private cleanReturnUrl(url: URL): URL {
    const base = this.baseUrl;
    url.searchParams.delete(authErrorParam);
    if (url.pathname === '/' || url.pathname === base.pathname.slice(0, -1)) {
      url.pathname = base.pathname;
    }
    return url;
  }

  private readAttempt(
    destination: URL,
    fromFailure = false
  ): SignInAttempt | undefined {
    if (this.pageState === 'completed') return;
    try {
      const saved = globalThis.sessionStorage.getItem(attemptKey);
      if (!saved) return;

      const attempt: Partial<SignInAttempt> | null = JSON.parse(saved);
      const now = Date.now();
      if (
        !attempt ||
        typeof attempt.returnUrl !== 'string' ||
        typeof attempt.expiresAt !== 'number' ||
        !Number.isFinite(attempt.expiresAt) ||
        attempt.expiresAt <= now ||
        attempt.expiresAt > now + attemptLifetimeMs ||
        (attempt.step !== 'login' &&
          attempt.step !== 'logout' &&
          attempt.step !== 'login-after-logout')
      ) {
        return;
      }

      const base = this.baseUrl;
      const url = new URL(attempt.returnUrl);
      if (
        url.origin !== base.origin ||
        url.protocol !== base.protocol ||
        url.username ||
        url.password ||
        !url.pathname.startsWith(base.pathname)
      ) {
        return;
      }

      const returnUrl = this.cleanReturnUrl(url).href;
      const returnedToBase =
        destination.href === base.href &&
        (fromFailure || attempt.step === 'logout');
      if (returnUrl === destination.href || returnedToBase) {
        return { returnUrl, step: attempt.step, expiresAt: attempt.expiresAt };
      }
    } catch {
      // Treat malformed or unavailable storage as a fresh attempt.
    }
  }

  private startRedirect(attempt: SignInAttempt) {
    const previousState = this.pageState;
    this.pageState = { redirectUntil: Date.now() + attemptLifetimeMs };
    this.saveAttempt(attempt);
    try {
      const base = this.baseUrl.pathname;
      globalThis.location.replace(
        attempt.step === 'logout'
          ? `${base}api/auth/logout?redirectUri=/`
          : `${base}api/auth/login?redirectUri=${encodeURIComponent(attempt.returnUrl)}`
      );
    } catch (error) {
      this.pageState = previousState;
      throw error;
    }
  }

  private saveAttempt(attempt: SignInAttempt | null) {
    try {
      globalThis.sessionStorage.setItem(attemptKey, JSON.stringify(attempt));
    } catch {
      // Storage is optional. The backend still reports confirmed sign-in failures.
    }
  }
}

export default new RedirectHandler();
