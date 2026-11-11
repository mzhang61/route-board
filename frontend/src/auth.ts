import { UserManager, WebStorageStateStore } from 'oidc-client-ts';

const authority: unknown = import.meta.env.VITE_OIDC_AUTHORITY;
const clientId: unknown = import.meta.env.VITE_OIDC_CLIENT_ID;
const scope: unknown = import.meta.env.VITE_OIDC_SCOPE;
const audience: unknown = import.meta.env.VITE_OIDC_AUDIENCE;
export const auth = typeof authority === 'string' && typeof clientId === 'string' && typeof scope === 'string'
  && authority && clientId && scope ? new UserManager({
    authority, client_id: clientId, scope: `openid ${scope}`,
    redirect_uri: `${window.location.origin}/auth/callback`, response_type: 'code',
    extraQueryParams: typeof audience === 'string' && audience ? { audience } : {},
    disablePKCE: false, loadUserInfo: false, automaticSilentRenew: false,
    userStore: new WebStorageStateStore({ store: window.sessionStorage }),
    stateStore: new WebStorageStateStore({ store: window.sessionStorage }),
  }) : null;

export async function accessToken(): Promise<string> {
  const user = await auth?.getUser();
  if (!user || user.expired) throw new Error('Sign in with a dispatcher account first.');
  return user.access_token;
}
