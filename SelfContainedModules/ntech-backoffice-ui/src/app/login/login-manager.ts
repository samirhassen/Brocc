import {PlatformLocation} from '@angular/common';
import {HttpClient} from '@angular/common/http';
import * as moment from 'moment';
import {parseQueryStringParameters, StringDictionary} from '../common.types';

export class LoginManager {
    private trailingSlash(fragment: string, wantTrailingSlash: boolean) {
        if (!fragment) {
            fragment = '';
        }
        if ((fragment.endsWith('/') && wantTrailingSlash) || (!fragment.endsWith('/') && !wantTrailingSlash)) {
            return fragment;
        }
        if (fragment.endsWith('/')) {
            return fragment.substr(0, fragment.length - 1);
        } else {
            return fragment + '/';
        }
    }

    public async getClientSettingsSource(
        httpClient: HttpClient,
        platform: PlatformLocation
    ): Promise<UserManagerSettingsSource> {
        let x = await httpClient
            .post<{ UserModuleRootUrl: string }>('/api/embedded-backoffice/fetch-auth-config', {})
            .toPromise();

        let userModuleRootUrl: string = x.UserModuleRootUrl;
        return new UserManagerSettingsSource(
            this.trailingSlash(userModuleRootUrl, true) + 'id',
            this.trailingSlash(document.location.origin, false) +
            platform.getBaseHrefFromDOM() +
            'login-complete'
        );
    }

    public async onInit(httpClient: HttpClient, platform: PlatformLocation): Promise<LoginInitResult> {
        let isCallback = document.location.pathname == '/s/login-complete';
        let clientSettingsSource = await this.getClientSettingsSource(httpClient, platform);

        let forceLogin = (lastState: LoginState) => {
            let currentEpoch = moment().valueOf();
            if (
                lastState?.lastLoginAttemptUnixEpoch &&
                currentEpoch - lastState.lastLoginAttemptUnixEpoch < 15 * 1000
            ) {
                //if we tried to login less than 15 seconds ago and are now trying again we assume there is some config erro
                //or similiar and the login is looping forever so we break the loop
                document.location.href = '/s/login-looping';
            } else {
                let manager = new UserManager(clientSettingsSource.getClientSettings(false));
                let state: LoginState = {
                    redirectAfterLoginUrl: decodeURIComponent(document.location.href),
                    lastLoginAttemptUnixEpoch: currentEpoch,
                    queryStringParameters: parseQueryStringParameters(),
                };
                manager.signinRedirect({state: JSON.stringify(state)});
            }
        };

        let initResult: LoginInitResult = {
            forceReLogin: forceLogin,
            wasRedirected: true,
            queryStringParameters: parseQueryStringParameters(),
        };
        if (isCallback) {
            let manager = new UserManager(clientSettingsSource.getClientSettings(true));
            window.location.hash = decodeURIComponent(window.location.hash);
            let loginResponse = await manager
                .processSigninResponse(decodeURIComponent(document.location.href).replace('#', '?'));
            if (!loginResponse || loginResponse.error) {
                document.location.href = '/'; //Send them to the backoffice startpage else
                return initResult;
            } else {
                initResult.user = loginResponse;
                let state = parseLoginState(loginResponse?.state);
                if (state && state.queryStringParameters) {
                    //Since we redirect the user the original query string is lost
                    initResult.queryStringParameters = state.queryStringParameters;
                }
                initResult.accessTokenExpirationEpoch = moment()
                    .add(loginResponse.expires_in, 'seconds')
                    .valueOf();
                cacheAuthSession(loginResponse, initResult.accessTokenExpirationEpoch);
                initResult.wasRedirected = false;
                return initResult;
            }

        } else {
            let cachedAuthSession = getCachedAuthSession();
            if (cachedAuthSession) {
                //Reuse non expired access token in the session
                if (cachedAuthSession.accessTokenExpirationEpoch > moment().valueOf()) {
                    initResult.user = cachedAuthSession.loginResponse;
                    initResult.accessTokenExpirationEpoch = cachedAuthSession.accessTokenExpirationEpoch;
                    initResult.wasRedirected = false;
                    return initResult;
                }
            }
            forceLogin(null);
            return initResult;
        }
    }
}

export function parseLoginState(rawState: string): LoginState {
    if (!rawState) {
        return null;
    }
    return JSON.parse(rawState);
}

const sessionTokenKey = 'ntech.session.v20210618.1';

interface AuthenticationSessionModel {
    version: string;
    loginResponse: SigninResponse;
    accessTokenExpirationEpoch: number;
}

function cacheAuthSession(loginResponse: SigninResponse, accessTokenExpirationEpoch: number) {
    sessionStorage.setItem(
        sessionTokenKey,
        JSON.stringify({
            version: sessionTokenKey,
            loginResponse: loginResponse,
            accessTokenExpirationEpoch: accessTokenExpirationEpoch,
        })
    );
}

function getCachedAuthSession(): AuthenticationSessionModel {
    let raw = sessionStorage.getItem(sessionTokenKey);
    if (raw) {
        let parsed = JSON.parse(sessionStorage.getItem(sessionTokenKey));
        if (parsed?.version === sessionTokenKey) {
            return parsed;
        }
    }
    return null;
}

export function clearCachedAuthSession() {
    sessionStorage.setItem(sessionTokenKey, '');
}

export interface LoginInitResult {
    wasRedirected: boolean;
    user?: SigninResponse;
    accessTokenExpirationEpoch?: number;
    forceReLogin: (lastState: LoginState) => void;
    queryStringParameters: StringDictionary;
}

export class UserManagerSettingsSource {
    constructor(private authority: string, private redirect_uri: string) {
    }

    public getClientSettings(isCallback: boolean): UserManagerSettings {
        let settings: any = {
            authority: this.authority,
            client_id: 'nBackOfficeEmbeddedUserLogin',
            redirect_uri: this.redirect_uri,
            response_type: 'id_token token',
            scope: 'openid nTech1',
        };

        if (isCallback) {
            settings.response_mode = 'query';
        }

        return settings;
    }
}

export interface LoginState {
    redirectAfterLoginUrl: string;
    lastLoginAttemptUnixEpoch: number; //used to detect looping, in milliseconds strictly increasing clock
    queryStringParameters: StringDictionary;
}

//Define oidc-client
declare const Version: string;

declare interface Logger {
    error(message?: any, ...optionalParams: any[]): void;

    info(message?: any, ...optionalParams: any[]): void;

    debug(message?: any, ...optionalParams: any[]): void;

    warn(message?: any, ...optionalParams: any[]): void;
}

declare interface AccessTokenEvents {

    load(container: User): void;

    unload(): void;

    /** Subscribe to events raised prior to the access token expiring */
    addAccessTokenExpiring(callback: (...ev: any[]) => void): void;

    removeAccessTokenExpiring(callback: (...ev: any[]) => void): void;

    /** Subscribe to events raised after the access token has expired */
    addAccessTokenExpired(callback: (...ev: any[]) => void): void;

    removeAccessTokenExpired(callback: (...ev: any[]) => void): void;
}

declare class InMemoryWebStorage {
    getItem(key: string): any;

    setItem(key: string, value: any): any;

    removeItem(key: string): any;

    key(index: number): any;

    length?: number;
}

declare class Log {
    static readonly NONE: 0;
    static readonly ERROR: 1;
    static readonly WARN: 2;
    static readonly INFO: 3;
    static readonly DEBUG: 4;

    static reset(): void;

    static level: number;

    static logger: Logger;

    static debug(message?: any, ...optionalParams: any[]): void;

    static info(message?: any, ...optionalParams: any[]): void;

    static warn(message?: any, ...optionalParams: any[]): void;

    static error(message?: any, ...optionalParams: any[]): void;
}

declare interface MetadataService {
    new(settings: OidcClientSettings): MetadataService;

    metadataUrl?: string;

    resetSigningKeys(): void;

    getMetadata(): Promise<OidcMetadata>;

    getIssuer(): Promise<string>;

    getAuthorizationEndpoint(): Promise<string>;

    getUserInfoEndpoint(): Promise<string>;

    getTokenEndpoint(optional?: boolean): Promise<string | undefined>;

    getCheckSessionIframe(): Promise<string | undefined>;

    getEndSessionEndpoint(): Promise<string | undefined>;

    getRevocationEndpoint(): Promise<string | undefined>;

    getKeysEndpoint(): Promise<string | undefined>;

    getSigningKeys(): Promise<any[]>;
}

declare interface MetadataServiceCtor {
    (settings: OidcClientSettings, jsonServiceCtor?: any): MetadataService;
}

declare interface ResponseValidator {
    validateSigninResponse(state: any, response: any): Promise<SigninResponse>;

    validateSignoutResponse(state: any, response: any): Promise<SignoutResponse>;
}

declare interface ResponseValidatorCtor {
    (settings: OidcClientSettings, metadataServiceCtor?: MetadataServiceCtor, userInfoServiceCtor?: any): ResponseValidator;
}

declare interface SigninRequest {
    url: string;
    state: any;
}

declare interface SignoutRequest {
    url: string;
    state?: any;
}

declare class OidcClient {
    constructor(settings: OidcClientSettings);

    readonly settings: OidcClientSettings;

    createSigninRequest(args?: any): Promise<SigninRequest>;

    processSigninResponse(url?: string, stateStore?: StateStore): Promise<SigninResponse>;

    createSignoutRequest(args?: any): Promise<SignoutRequest>;

    processSignoutResponse(url?: string, stateStore?: StateStore): Promise<SignoutResponse>;

    clearStaleState(stateStore: StateStore): Promise<any>;

    readonly metadataService: MetadataService;
}

declare interface OidcClientSettings {
    /** The URL of the OIDC/OAuth2 provider */
    authority?: string;
    readonly metadataUrl?: string;
    /** Provide metadata when authority server does not allow CORS on the metadata endpoint */
    metadata?: Partial<OidcMetadata>;
    /** Provide signingKeys when authority server does not allow CORS on the jwks uri */
    signingKeys?: any[];
    /** Can be used to seed or add additional values to the results of the discovery request */
    metadataSeed?: Partial<OidcMetadata>;
    /** Your client application's identifier as registered with the OIDC/OAuth2 */
    client_id?: string;
    client_secret?: string;
    /** The type of response desired from the OIDC/OAuth2 provider (default: 'id_token') */
    readonly response_type?: string;
    readonly response_mode?: string;
    /** The scope being requested from the OIDC/OAuth2 provider (default: 'openid') */
    readonly scope?: string;
    /** The redirect URI of your client application to receive a response from the OIDC/OAuth2 provider */
    readonly redirect_uri?: string;
    /** The OIDC/OAuth2 post-logout redirect URI */
    readonly post_logout_redirect_uri?: string;
    /** The OIDC/OAuth2 post-logout redirect URI when using popup */
    readonly popup_post_logout_redirect_uri?: string;
    readonly prompt?: string;
    readonly display?: string;
    readonly max_age?: number;
    readonly ui_locales?: string;
    readonly acr_values?: string;
    /** Should OIDC protocol claims be removed from profile (default: true) */
    readonly filterProtocolClaims?: boolean;
    /** Flag to control if additional identity data is loaded from the user info endpoint in order to populate the user's profile (default: true) */
    readonly loadUserInfo?: boolean;
    /** Number (in seconds) indicating the age of state entries in storage for authorize requests that are considered abandoned and thus can be cleaned up (default: 300) */
    readonly staleStateAge?: number;
    /** The window of time (in seconds) to allow the current time to deviate when validating id_token's iat, nbf, and exp values (default: 300) */
    readonly clockSkew?: number;
    readonly clockService?: ClockService;
    readonly stateStore?: StateStore;
    readonly userInfoJwtIssuer?: 'ANY' | 'OP' | string;
    readonly mergeClaims?: boolean;
    ResponseValidatorCtor?: ResponseValidatorCtor;
    MetadataServiceCtor?: MetadataServiceCtor;
    /** An object containing additional query string parameters to be including in the authorization request */
    extraQueryParams?: Record<string, any>;
}

declare class UserManager extends OidcClient {
    constructor(settings: UserManagerSettings);

    readonly settings: UserManagerSettings;

    /** Removes stale state entries in storage for incomplete authorize requests */
    clearStaleState(): Promise<void>;

    /** Load the User object for the currently authenticated user */
    getUser(): Promise<User | null>;

    storeUser(user: User): Promise<void>;

    /** Remove from any storage the currently authenticated user */
    removeUser(): Promise<void>;

    /** Trigger a request (via a popup window) to the authorization endpoint. The result of the promise is the authenticated User */
    signinPopup(args?: any): Promise<User>;

    /** Notify the opening window of response from the authorization endpoint */
    signinPopupCallback(url?: string): Promise<User | undefined>;

    /** Trigger a silent request (via an iframe or refreshtoken if available) to the authorization endpoint */
    signinSilent(args?: any): Promise<User>;

    /** Notify the parent window of response from the authorization endpoint */
    signinSilentCallback(url?: string): Promise<User | undefined>;

    /** Trigger a redirect of the current window to the authorization endpoint */
    signinRedirect(args?: any): Promise<void>;

    /** Process response from the authorization endpoint. */
    signinRedirectCallback(url?: string): Promise<User>;

    /** Trigger a redirect of the current window to the end session endpoint */
    signoutRedirect(args?: any): Promise<void>;

    /** Process response from the end session endpoint */
    signoutRedirectCallback(url?: string): Promise<SignoutResponse>;

    /** Trigger a redirect of a popup window window to the end session endpoint */
    signoutPopup(args?: any): Promise<void>;

    /** Process response from the end session endpoint from a popup window */
    signoutPopupCallback(url?: string, keepOpen?: boolean): Promise<void>;
    signoutPopupCallback(keepOpen?: boolean): Promise<void>;

    /** Proxy to Popup, Redirect and Silent callbacks */
    signinCallback(url?: string): Promise<User>;

    /** Proxy to Popup and Redirect callbacks */
    signoutCallback(url?: string, keepWindowOpen?: boolean): Promise<SignoutResponse | undefined>;

    /** Query OP for user's current signin status */
    querySessionStatus(args?: any): Promise<SessionStatus>;

    revokeAccessToken(): Promise<void>;

    /** Enables silent renew  */
    startSilentRenew(): void;

    /** Disables silent renew */
    stopSilentRenew(): void;

    events: UserManagerEvents;
}

declare interface SessionStatus {
    /** Opaque session state used to validate if session changed (monitorSession) */
    session_state: string;
    /** Subject identifier */
    sub: string;
    /** Session ID */
    sid?: string;
}

declare interface UserManagerEvents extends AccessTokenEvents {
    load(user: User): any;

    unload(): any;
}

declare interface UserManagerSettings extends OidcClientSettings {
    /** The URL for the page containing the call to signinPopupCallback to handle the callback from the OIDC/OAuth2 */
    readonly popup_redirect_uri?: string;
    /** The features parameter to window.open for the popup signin window.
     *  default: 'location=no,toolbar=no,width=500,height=500,left=100,top=100'
     */
    readonly popupWindowFeatures?: string;
    /** The target parameter to window.open for the popup signin window (default: '_blank') */
    readonly popupWindowTarget?: any;
    /** The URL for the page containing the code handling the silent renew */
    readonly silent_redirect_uri?: any;
    /** Number of milliseconds to wait for the silent renew to return before assuming it has failed or timed out (default: 10000) */
    readonly silentRequestTimeout?: any;
    /** Flag to indicate if there should be an automatic attempt to renew the access token prior to its expiration (default: false) */
    readonly automaticSilentRenew?: boolean;
    readonly validateSubOnSilentRenew?: boolean;
    /** Flag to control if id_token is included as id_token_hint in silent renew calls (default: true) */
    readonly includeIdTokenInSilentRenew?: boolean;
    /** Will raise events for when user has performed a signout at the OP (default: true) */
    readonly monitorSession?: boolean;
    /** Interval, in ms, to check the user's session (default: 2000) */
    readonly checkSessionInterval?: number;
    readonly query_status_response_type?: string;
    readonly stopCheckSessionOnError?: boolean;
    /** Will invoke the revocation endpoint on signout if there is an access token for the user (default: false) */
    readonly revokeAccessTokenOnSignout?: boolean;
    /** The number of seconds before an access token is to expire to raise the accessTokenExpiring event (default: 60) */
    readonly accessTokenExpiringNotificationTime?: number;
    readonly redirectNavigator?: any;
    readonly popupNavigator?: any;
    readonly iframeNavigator?: any;
    /** Storage object used to persist User for currently authenticated user (default: session storage) */
    readonly userStore?: WebStorageStateStore;
}

declare interface ClockService {
    getEpochTime(): Promise<number>;
}

declare interface WebStorageStateStoreSettings {
    prefix?: string;
    store?: any;
}

declare interface StateStore {
    set(key: string, value: any): Promise<void>;

    get(key: string): Promise<any>;

    remove(key: string): Promise<any>;

    getAllKeys(): Promise<string[]>;
}

declare class WebStorageStateStore implements StateStore {
    constructor(settings: WebStorageStateStoreSettings);

    set(key: string, value: any): Promise<void>;

    get(key: string): Promise<any>;

    remove(key: string): Promise<any>;

    getAllKeys(): Promise<string[]>;
}

declare interface SigninResponse {
    new(url: string, delimiter?: string): SigninResponse;

    access_token: string;
    /** Refresh token returned from the OIDC provider (if requested, via the
     * 'offline_access' scope) */
    refresh_token?: string;
    code: string;
    error: string;
    error_description: string;
    error_uri: string;
    id_token: string;
    profile: any;
    scope: string;
    session_state: string;
    state: any;
    token_type: string;

    readonly expired: boolean | undefined;
    readonly expires_in: number | undefined;
    readonly isOpenIdConnect: boolean;
    readonly scopes: string[];
}

declare interface SignoutResponse {
    new(url: string): SignoutResponse;

    error?: string;
    error_description?: string;
    error_uri?: string;
    state?: any;
}

declare interface UserSettings {
    id_token: string;
    session_state: string;
    access_token: string;
    refresh_token: string;
    token_type: string;
    scope: string;
    profile: Profile;
    expires_at: number;
    state: any;
}

declare class User {
    constructor(settings: UserSettings);

    /** The id_token returned from the OIDC provider */
    id_token: string;
    /** The session state value returned from the OIDC provider (opaque) */
    session_state?: string;
    /** The access token returned from the OIDC provider. */
    access_token: string;
    /** Refresh token returned from the OIDC provider (if requested) */
    refresh_token?: string;
    /** The token_type returned from the OIDC provider */
    token_type: string;
    /** The scope returned from the OIDC provider */
    scope: string;
    /** The claims represented by a combination of the id_token and the user info endpoint */
    profile: Profile;
    /** The expires at returned from the OIDC provider */
    expires_at: number;
    /** The custom state transferred in the last signin */
    state: any;

    toStorageString(): string;

    static fromStorageString(storageString: string): User;

    /** Calculated number of seconds the access token has remaining */
    readonly expires_in: number;
    /** Calculated value indicating if the access token is expired */
    readonly expired: boolean;
    /** Array representing the parsed values from the scope */
    readonly scopes: string[];
}

declare type Profile = IDTokenClaims & ProfileStandardClaims;

interface IDTokenClaims {
    /** Issuer Identifier */
    iss: string;
    /** Subject identifier */
    sub: string;
    /** Audience(s): client_id ... */
    aud: string;
    /** Expiration time */
    exp: number;
    /** Issued at */
    iat: number;
    /** Time when the End-User authentication occurred */
    auth_time?: number;
    /** Time when the End-User authentication occurred */
    nonce?: number;
    /** Access Token hash value */
    at_hash?: string;
    /** Authentication Context Class Reference */
    acr?: string;
    /** Authentication Methods References */
    amr?: string[];
    /** Authorized Party - the party to which the ID Token was issued */
    azp?: string;
    /** Session ID - String identifier for a Session */
    sid?: string;

    /** Other custom claims */
    [claimKey: string]: any;
}

interface ProfileStandardClaims {
    /** End-User's full name */
    name?: string;
    /** Given name(s) or first name(s) of the End-User */
    given_name?: string;
    /** Surname(s) or last name(s) of the End-User */
    family_name?: string;
    /** Middle name(s) of the End-User */
    middle_name?: string;
    /** Casual name of the End-User that may or may not be the same as the given_name. */
    nickname?: string;
    /** Shorthand name that the End-User wishes to be referred to at the RP, such as janedoe or j.doe. */
    preferred_username?: string;
    /** URL of the End-User's profile page */
    profile?: string;
    /** URL of the End-User's profile picture */
    picture?: string;
    /** URL of the End-User's Web page or blog */
    website?: string;
    /** End-User's preferred e-mail address */
    email?: string;
    /** True if the End-User's e-mail address has been verified; otherwise false. */
    email_verified?: boolean;
    /** End-User's gender. Values defined by this specification are female and male. */
    gender?: string;
    /** End-User's birthday, represented as an ISO 8601:2004 [ISO8601‑2004] YYYY-MM-DD format */
    birthdate?: string;
    /** String from zoneinfo [zoneinfo] time zone database representing the End-User's time zone. */
    zoneinfo?: string;
    /** End-User's locale, represented as a BCP47 [RFC5646] language tag. */
    locale?: string;
    /** End-User's preferred telephone number. */
    phone_number?: string;
    /** True if the End-User's phone number has been verified; otherwise false. */
    phone_number_verified?: boolean;
    /** object 	End-User's preferred address in JSON [RFC4627] */
    address?: OidcAddress;
    /** Time the End-User's information was last updated. */
    updated_at?: number;
}

interface OidcAddress {
    /** Full mailing address, formatted for display or use on a mailing label */
    formatted?: string;
    /** Full street address component, which MAY include house number, street name, Post Office Box, and multi-line extended street address information */
    street_address?: string;
    /** City or locality component */
    locality?: string;
    /** State, province, prefecture, or region component */
    region?: string;
    /** Zip code or postal code component */
    postal_code?: string;
    /** Country name component */
    country?: string;
}

declare class CordovaPopupWindow {
    constructor(params: any);

    navigate(params: any): Promise<any>;

    promise: Promise<any>;
}

declare class CordovaPopupNavigator {
    prepare(params: any): Promise<CordovaPopupWindow>;
}

declare class CordovaIFrameNavigator {
    prepare(params: any): Promise<CordovaPopupWindow>;
}

declare interface OidcMetadata {
    issuer: string;
    authorization_endpoint: string;
    token_endpoint: string;
    token_endpoint_auth_methods_supported: string[];
    token_endpoint_auth_signing_alg_values_supported: string[];
    userinfo_endpoint: string;
    check_session_iframe: string;
    end_session_endpoint: string;
    jwks_uri: string;
    registration_endpoint: string;
    scopes_supported: string[];
    response_types_supported: string[];
    acr_values_supported: string[];
    subject_types_supported: string[];
    userinfo_signing_alg_values_supported: string[];
    userinfo_encryption_alg_values_supported: string[];
    userinfo_encryption_enc_values_supported: string[];
    id_token_signing_alg_values_supported: string[];
    id_token_encryption_alg_values_supported: string[];
    id_token_encryption_enc_values_supported: string[];
    request_object_signing_alg_values_supported: string[];
    display_values_supported: string[];
    claim_types_supported: string[];
    claims_supported: string[];
    claims_parameter_supported: boolean;
    service_documentation: string;
    ui_locales_supported: string[];

    revocation_endpoint: string;
    introspection_endpoint: string;
    frontchannel_logout_supported: boolean;
    frontchannel_logout_session_supported: boolean;
    backchannel_logout_supported: boolean;
    backchannel_logout_session_supported: boolean;
    grant_types_supported: string[];
    response_modes_supported: string[];
    code_challenge_methods_supported: string[];
}

declare interface CheckSessionIFrame {
    new(callback: () => void, client_id: string, url: string, interval?: number, stopOnError?: boolean): CheckSessionIFrame

    load(): Promise<void>;

    start(session_state: string): void;

    stop(): void;
}

declare interface CheckSessionIFrameCtor {
    (callback: () => void, client_id: string, url: string, interval?: number, stopOnError?: boolean): CheckSessionIFrame;
}

declare class SessionMonitor {
    constructor(userManager: UserManager, CheckSessionIFrameCtor: CheckSessionIFrameCtor);
}
