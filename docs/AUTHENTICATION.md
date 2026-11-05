# SprintFlow — Authentication

> **Describes the system as it works today.** Everything below is implemented and verified in the
> repository. This is not a changelog and not a plan.
>
> Domain model (Areas, Boards, Teams, Tasks) is a separate concern — see
> [`DOMAIN_MODEL.md`](DOMAIN_MODEL.md).

---

## 1. Authentication vs authorization

| Question | Where it is answered |
|---|---|
| **Who is this user?** | Authentication — login, BCrypt, JWT. Implemented. |
| **What global user type is this?** | `Admin` or `Member`, stored in `Roles`, carried in the token. |
| **What may this user do with a specific object?** | Scoped authorization. **Not designed, not implemented.** |

Keep these separate. Being authenticated is not the same as being authorized.

---

## 2. Roles

Two roles exist, seeded with the schema:

| RoleId | RoleName |
|---|---|
| 1 | `Admin` |
| 2 | `Member` |

`Admin` is the highest-privileged global user type. The name `User` is not used — `Member` is the
agreed term.

Roles are a separate table joined by a foreign key, never a string column on the user:

```
Users.RoleId  →  Roles.RoleId  →  Roles.RoleName  →  JWT ClaimTypes.Role
     (2)             (2)              "Member"              "Member"
```

The role claim carries the **name**, not the numeric id, because `[Authorize(Roles = "Admin")]`
compares strings. Reading a user for token creation therefore requires `Include(u => u.Role)`.

Constants live in `Common/Constants/RoleNames.cs`.

---

### Schema

Two tables exist today — `Roles` and `Users`. The authoritative definition is
`Data/Configurations/RoleConfiguration.cs`, `Data/Configurations/UserConfiguration.cs` and the
`InitialCreate` / `SetEmailCollation` migrations; this is a summary.

| Table | Columns | Constraints |
|---|---|---|
| `Roles` | `RoleId` (PK, identity), `RoleName` nvarchar(50) | unique `IX_Roles_RoleName` |
| `Users` | `UserId` (PK, identity), `FirstName`/`LastName` nvarchar(100), `Email` nvarchar(256), `PasswordHash` nvarchar(256), `RoleId`, `IsActive` bit default `1`, `CreatedDate` datetime2 default `SYSUTCDATETIME()` | unique `IX_Users_Email`, `IX_Users_RoleId`, `IX_Users_IsActive`, FK `Users.RoleId → Roles.RoleId` with `Restrict` |

`FullName` is composed at read time from `FirstName` + `LastName` and is deliberately not a column.

A disabled account (`IsActive = false`) cannot sign in, and the rejection is indistinguishable from
a wrong password.

## 3. The Admin decision

The dashboard a user lands on is decided by comparing the **authenticated `UserId`** with the
configured admin id:

```csharp
var isAdmin = user.UserId == authOptions.AdminUserId;   // Services/AuthService.cs
```

- The value comes from `Auth:AdminUserId` in `appsettings.json` (currently `1`), bound through
  `Options/AuthOptions.cs`.
- The decision is made **on the server** and returned to the client as a boolean.
- It is **never** based on the e-mail address, and **never** on the role name.
- Angular applies the answer; it does not re-derive it.

Startup refuses to run if this is misconfigured — see §7.

---

## 4. Password handling

- **BCrypt**, work factor **12** (`Authentication/BCryptPasswordHasher.cs`).
- Plaintext passwords are never stored. Seeded users carry constant `$2a$12$…` hashes.
- A password hash never appears in any API response.

### Timing protection

When no user matches the submitted e-mail, verification still runs against a **dummy hash**
generated once at startup:

```csharp
passwordHasher.VerifyAgainstDummy(request.Password);   // Services/AuthService.cs
throw new AuthenticationFailedException();
```

Without this, response time would reveal whether an address is registered.

The password is also verified **before** `IsActive` is examined, so a disabled account cannot be
distinguished from a wrong password by timing.

---

## 5. E-mail handling

### Canonicalization

```csharp
public static string CanonicalizeEmail(string email) => email.Trim().ToLowerInvariant();
```

`ToLowerInvariant`, never `ToLower`: the latter follows the machine culture and turns `'I'` into
`'ı'` on a Turkish machine, which would break the lookup.

Seeding, login and any future registration must use this same rule.

### Collation

`Users.Email` is pinned to **`Latin1_General_100_CI_AS`**, overriding the database default
`Turkish_CI_AS` (applied by the `SetEmailCollation` migration).

Under Turkish collation `i` and `I` are different letters, so `AYSE.DEMIR@…` would not match
`ayse.demir@…` on login, and the unique index would allow both as separate accounts. Under the
pinned collation `i`/`I` differ only by case, while `İ` (U+0130) and `ı` (U+0131) remain distinct
characters. Verified to behave identically under `SET LANGUAGE Türkçe` and `us_english`.

E-mail is a credential only. It plays no part in the Admin decision.

---

## 6. JWT

| Setting | Value |
|---|---|
| Algorithm | **HS256** |
| Lifetime | **8 hours** (`Jwt:ExpiryMinutes` = 480) |
| `ClockSkew` | **`TimeSpan.Zero`** |
| Refresh token | None |
| Issuer / Audience | `SprintFlow.Api` / `SprintFlow.Client` |

### Claims

| Claim | Value |
|---|---|
| `sub` | UserId |
| `ClaimTypes.NameIdentifier` | UserId |
| `ClaimTypes.Role` | `Admin` or `Member` |
| `email` | canonical e-mail |
| `jti` | GUID |
| `exp` | +8 hours |

`iat` and `nbf` are added by the token handler; they are standard and left as-is.

**A JWT is signed, not encrypted.** Anyone holding the token can read every claim. The signature
only proves the payload was not altered. Claims therefore carry identity, never secrets.

### Configuration keys

| Key | Where |
|---|---|
| `Jwt:Key` | **User secrets** locally (`dotnet user-secrets set "Jwt:Key" <value> --project SprintFlow.Web.Server`); the `Jwt__Key` environment variable in production. **Never in a tracked file, and no longer in `appsettings.Development.json`** — `SprintFlow.Web.SignalR` needs the same value and holds its own copy in its own user secrets. |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | `appsettings.json` |

The key names are `Jwt:Key` and `Jwt:ExpiryMinutes`.

### Issuing vs validating

Two separate responsibilities reading the same configuration section:

- **Issuing** — `Authentication/JwtTokenService.cs`
- **Validating** — `AddJwtBearer` in `Extensions/ServiceCollectionExtensions.cs`

`RoleClaimType` and `NameClaimType` are set explicitly so authorization does not depend on the
framework's default claim-name mapping.

---

## 7. Startup configuration validation

`Extensions/StartupValidationExtensions.cs` runs after the host is built and **refuses to start** on:

| Check | Failure |
|---|---|
| `Jwt:Key` present and ≥ 32 characters | Startup fails |
| `Jwt:Issuer` and `Jwt:Audience` set | Startup fails |
| `Jwt:ExpiryMinutes` > 0 | Startup fails |
| `Auth:AdminUserId` present and > 0 | Startup fails |
| A user with that id exists | Startup fails |
| That user's role is `Admin` | Startup fails |
| `Invitations:ExpiryHours` > 0 | Startup fails — zero or negative would mint links that have already expired, and every registration would fail as though registration itself were broken |

A misconfigured `Auth:AdminUserId` crashes nothing on its own — it silently makes the wrong person
admin, or nobody. That class of fault survives testing, so it is turned into a startup failure.
There is deliberately **no fallback** to "some other admin".

---

## 8. The login endpoint

```
POST /api/auth/login
Content-Type: application/json

Request  : { "email": string, "password": string }
200      : { "token": string, "fullName": string, "isAdmin": boolean }
400      : ProblemDetails + errors{}    (model validation)
401      : ProblemDetails, detail = "Invalid e-mail or password."
```

Flow (`Controllers/AuthController.cs` → `Services/AuthService.cs`):

```
canonicalize e-mail
  → look up user (AsNoTracking, Include(Role))
  → not found  → dummy-hash verification → 401
  → verify password with BCrypt
  → password wrong OR account inactive → 401
  → isAdmin = UserId == Auth:AdminUserId
  → issue JWT
  → 200 { token, fullName, isAdmin }
```

### Failure behaviour

Unknown e-mail, wrong password and disabled account all produce **the same 401 with the same
message**. Which of the three occurred is never revealed — not in the body, not in the status, and
not through response timing.

`Middleware/ExceptionHandlingMiddleware.cs` converts `AuthenticationFailedException` into an
RFC 7807 `ProblemDetails` 401. Unexpected exceptions become a plain 500; the real detail goes to the
log, never to the client. No stack trace reaches the caller.

Failed sign-in attempts are logged at **Warning** level without the submitted e-mail — see
[§11](#11-logging).

---

## 8a. Registration by invitation

There is no public sign-up. An account can only be created by redeeming a **single-use, expiring
invitation** that an already-authenticated user generated.

Every account exists because someone already inside the system chose to invite that person. The login page has no register link, and
`/register` is reachable only by holding a token.

### Who may invite

Any signed-in user. `POST /api/invitations` carries `[Authorize]` and **no role requirement** — a
Member invites exactly as an Admin does. There is no permission system behind it.

### The token

- 32 bytes from `RandomNumberGenerator`, Base64Url-encoded. Carries no identity, no address, no
  timestamp.
- **Only its SHA-256 digest is stored**, in `Invitations.TokenHash`. The plaintext is returned once,
  at creation, and is never stored and never logged.
- SHA-256 rather than BCrypt because the token is 256 bits of uniform randomness — nothing to
  brute-force, no salt needed — and a salted hash could not be indexed for lookup. **Passwords keep
  BCrypt at work factor 12.** These are two different problems.
- The column is **`varbinary(32)`**, not text: the database collates `Turkish_CI_AS`, which is
  case-insensitive, so a Base64 string column could treat two different digests as equal. Same
  family of problem as the `Users.Email` collation pin in §5.
- Lifetime comes from `Invitations:ExpiryHours` in `appsettings.json` (default 168 = 7 days), and
  expiry is compared against `DateTime.UtcNow` — an instant, so it does **not** go through
  `IApplicationClock`, which decides calendar days.

### Endpoints

```
POST /api/invitations           [Authorize], no body
201  : { "token": string, "expiresAt": string }

GET  /api/invitations/{token}   anonymous
204  : usable
400  : ProblemDetails - not valid / already used / expired / inviter disabled

POST /api/auth/register         anonymous, inert without a usable token
Request : { token, firstName, lastName, email, password }
201     : { "fullName": string, "email": string }
400     : ProblemDetails, or ProblemDetails + errors{} for field validation
```

The registration URL is composed **by the browser** from `window.location.origin`, not by the
server. The server would have to trust the request's `Host` header, and no backend host is resolved
on the server or hard-coded in the client — the same rule that keeps `apiBaseUrl` relative (§9).

### What registration cannot do

The request DTO has **no** property for a role, a user id, an active flag or any ownership. There is
nothing to ignore and nothing to forget to check — a new account is always `Member`. And admin
access is not a role anyway: it is `UserId == Auth:AdminUserId` (§3), which nothing here touches.

### Single use, under concurrency

Reading `UsedAt` and then writing it is not enough — under `READ COMMITTED` two requests can both
read it null. Redemption runs inside `IInvitationRepository.RunRedemptionAsync`, which reuses the
gate `RunBoardOrderingAsync` established:

```sql
SELECT TOP 1 InvitationId FROM Invitations WITH (UPDLOCK, HOLDLOCK) WHERE TokenHash = @hash
```

The second caller blocks until the first commits, then sees the invitation spent. The account and
the consumption are written in one transaction. A **filtered unique index** on `RegisteredUserId` is
the independent database-level guarantee that one invitation can never yield two accounts.

Duplicate e-mail is guarded the same way: `EmailExistsAsync` gives a clear message, but
`IX_Users_Email` is the authority — a lost race surfaces as `SqlException` 2601/2627 and is
translated into the ordinary "e-mail already registered" 400 rather than a 500.

### Password rules

**Minimum 8 characters. No composition requirements.** That is the entire policy.

Separately — and not a strength rule — a password may be at most **72 UTF-8 bytes**, because that is
all bcrypt reads. Verified against BCrypt.Net-Next 4.2.0: a longer password does **not** raise an
error, it is silently truncated, so two passwords sharing their first 72 bytes verify against the
same hash. The limit is counted in bytes, never characters: `ş ğ ı ö ü ç İ` are two bytes each, so a
40-character Turkish password is 80 bytes and is rejected.

### Who invited whom

`Invitations` records `InvitedByUserId` and `RegisteredUserId`. **The `Users` table is unchanged.**
The inviter's e-mail is reached by joining rather than copied, because users are never deleted
(FR-U12; both foreign keys are `Restrict`) and e-mail editing (FR-U5) does not exist, so a snapshot
could not yet differ from the join. **If FR-U5 is implemented, add an `InviterEmail` column** — until
then the join is the accurate answer and a copy would be storing what can be computed.

### Registration does not sign anyone in

No token is issued and no session is created. The page navigates to `/login`, and the new account
authenticates through §8 exactly like every other one. There is one token issuer and one place a
session begins.

---

## 9. Angular authentication

| File | Responsibility |
|---|---|
| `core/services/auth.service.ts` | Login request, session state as signals |
| `core/services/token-storage.ts` | The only class that touches `localStorage` |
| `core/guards/auth-guard.ts` | Is anyone signed in? |
| `core/guards/admin-guard.ts` | Is this user admin? |
| `core/interceptors/auth-interceptor.ts` | Attaches the bearer token to API requests |
| `core/core-module.ts` | Registers the application singletons and the interceptor |
| `core/models/auth-models.ts` | `LoginRequest`, `LoginResponse`, `AuthSession` |

### Session and token storage

The session — `{ token, fullName, isAdmin }` — is stored in `localStorage` under a single key, and
`TokenStorage` is the only place that reads or writes it. Anything unreadable (malformed JSON, a
value from an older version, storage blocked by the browser) is treated as "not signed in" and
cleared, so a bad entry cannot break start-up.

`AuthService` seeds its session signal from storage, so **a page refresh keeps the user signed in**.

`localStorage` survives refreshes and closed tabs, which is what makes the session persistent. The
trade-off is that any script on the page can read it. This is accepted because the token is
short-lived and the server re-validates it on every request.

### Routing

```
/login                    → no guard
/dashboard, /users, …     → authGuard
/admin/…                  → authGuard + adminGuard
```

Admin lands on `/admin/dashboard`, Member on `/dashboard`, based on the `isAdmin` the **backend**
returned. Angular never decodes the token and never compares a role string. No admin identity —
no e-mail, no password, no `AdminUserId` — exists anywhere in Angular source.

### Guards are not a security boundary

`isAdmin` lives in browser storage and can be tampered with. Guards only stop a user from landing on
a screen they cannot use. Bypassing a guard shows the *screen*, never the *data* — the data comes
from the server, which validates the token on every request.

### Login errors

The component shows fixed local strings, not the server's text: `Invalid e-mail or password.` for
401, and a generic message for anything else. The submit button is disabled while the request is in
flight. A failed login never navigates away.

### Logout

`AuthService.logout()` clears storage and the session signal. There is no server call — the token is
stateless and has no revocation endpoint. Both shells clear the session **before** navigating to
`/login`, so a browser "back" is rejected by `authGuard`.

### API path

Angular calls the API with the relative path `/api/...` (`environments/environment.ts` →
`apiBaseUrl: '/api'`). In development the Angular dev server proxies `/api` to ASP.NET
(`src/proxy.conf.js`); in production both are served from one origin, so the same path works with no
proxy. No backend host is hard-coded anywhere in the client.

### The HTTP interceptor

`core/interceptors/auth-interceptor.ts` attaches `Authorization: Bearer <token>` to outgoing
requests. It is registered in `CoreModule` through the `HTTP_INTERCEPTORS` multi-provider — a class
interceptor rather than a functional one, because `AppModule` imports `HttpClientModule`, which has
no `withInterceptors` equivalent.

Two deliberate details:

- It injects **`TokenStorage`, not `AuthService`.** `AuthService` injects `HttpClient`, and
  `HttpClient` depends on the interceptor chain, so injecting it here would be a dependency cycle.
  Storage is always current: `AuthService` writes it on login and clears it on logout.
- It only touches requests whose URL starts with `environment.apiBaseUrl`. A request with no stored
  token, or one going anywhere else, passes through untouched.

Responses are passed straight through. There is deliberately **no global 401 handling and no
automatic sign-out** — each caller decides what a failure means for its own screen.

---

## 10. Development credentials

Five seeded users — 1 Admin (`UserId = 1`) and 4 Members — with invented identities. Plaintext
credentials are documented in `docs/DEV_CREDENTIALS.md`, which is git-ignored. Passwords never
appear in Angular source or in any tracked file.

---

## 11. Logging

Failed authentication is logged at **Warning** level. The log records that a sign-in was rejected —
it does **not** record the submitted e-mail, which field was wrong, the password, the hash or any
token. Messages that would assist account enumeration are not produced.
---

## 12. Rejected approaches — do not reintroduce

| Rejected | Why |
|---|---|
| Role name `User` | The agreed term is `Member` |
| Identifying the admin by e-mail address | The decision is `UserId == Auth:AdminUserId` |
| `if (role === 'Admin')` in Angular | The backend decides; the client applies |
| Hard-coding admin identity in Angular | No e-mail, password or `AdminUserId` in client source |
| Generic `IRepository<T>` / `Repository<T>` | Premature abstraction for a single query |
| `IUnitOfWork` | Login writes nothing |
| `ICurrentUserService` | The controllers that need the caller read `ClaimTypes.NameIdentifier` from `User` directly — a wrapper would add a layer without removing anything |
| Revealing which credential was wrong | Enables account enumeration |

---

## 13. Not part of authentication today

Refresh tokens · password reset · e-mail verification · sending invitations by e-mail · listing or
revoking an outstanding invitation · rate-limiting invitation creation · server-side token
revocation on logout · `guestGuard` · global 401 handling in the interceptor · scoped
Area/Board/Team/Task authorization.

`UseAuthentication()` and `UseAuthorization()` are in the pipeline and correctly ordered. Every
endpoint except `POST /api/auth/login`, `POST /api/auth/register` and
`GET /api/invitations/{token}` carries `[Authorize]` — and the two registration endpoints are inert
without a valid invitation token, so the whole API is closed to anonymous
callers. That is the full extent of server-side authorization: `AddAuthorization()` is called with
**no policies**, and no endpoint carries a role requirement or an ownership check. Who may act on a
*particular* Area, Board, Team or Task is not designed — see
[`DOMAIN_MODEL.md`](DOMAIN_MODEL.md) § *Ownership is not authorization*.
