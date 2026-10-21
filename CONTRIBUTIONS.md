# Contributions and engineering evidence

Original project: [lychnellj/wallet-api](https://github.com/lychnellj/wallet-api)  
Fork: [princly25/wallet-api](https://github.com/princly25/wallet-api)

This project is a fork of an existing ASP.NET Core wallet demonstration. Features already provided by the original author include PostgreSQL persistence, wallet transfers, JWT-based demo login, Docker Compose and several tests. **A fork alone does not imply these were independently implemented here.**

## Phase 1 — Security baseline

Implemented on the `feature/security-baseline` branch:

- Require the **Admin** role for wallet and user record access and money-moving endpoints. This is intentionally restrictive until verified user identities and wallet-ownership policies are implemented.
- Keep hardcoded demonstration login and seed data available only in ASP.NET **Development**.
- Remove a hardcoded database connection and signing key from shared `appsettings.json`. Production must supply secure `ConnectionStrings__PostgresConnection` and `Jwt__Key` values.
- Add HTTP integration tests asserting anonymous access is denied, regular users are forbidden, and admin access remains possible.
- Upgrade GitHub Actions from .NET SDK 8 to 10 and execute unit and API integration tests against Docker Compose + PostgreSQL.

**Validation status:** These commits are awaiting automated CI. Do not assume they are passing or production safe until tested.

## Known limitations

- Demo user credentials and development JWT key are intentionally public **only for local demonstrations**. Never expose a development-mode instance on the public internet.
- Real authentication, owner-based authorization, audit logging and permission policies are not implemented.
- Cross-wallet debit/credit and ledger writes must be made atomic and safe under concurrency; idempotency keys are not yet implemented.
- Database initialization uses EF Core `EnsureCreated` in Development, not production migrations.
- SQL Server, S3/Lambda, ECS/RDS deployment and AWS credentials are not configured.
- The upstream repository has no root LICENSE file; retain its attribution and source relationship. Do not assume an unrestricted license to redistribute externally.

## Planned next steps

1. Build and fix CI failures.
2. Replace admin-only stopgap with authenticated identity and safe wallet ownership verification.
3. Persist idempotency keys and make transfers atomic with concurrency tests.
4. Add SQL Server/PostgreSQL provider switch and run tests against both.
5. Implement S3-triggered Lambda statement ingestion with synthetic test data.
6. Add least-privilege OIDC CI/CD and optional AWS ECS/RDS deployment.

Keep commits tied to real changes and validated functionality, not cosmetic activity to simulate years of experience.
