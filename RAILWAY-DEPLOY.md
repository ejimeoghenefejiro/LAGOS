# Railway hosted demo

Deploy from `C:\LAGOS`, where Dockerfile and railway.toml live. One container serves
the compiled React site and .NET API. Frontend API calls use the same origin.
Railway must point its domain at the service's assigned PORT. `/health` checks SQL connectivity.

This package is for a restricted demo. SMS/WhatsApp delivery and official tax
verification remain simulated. Real-customer operation requires real delivery
providers and a separate production readiness review. OTP storage remains readable
as explicitly requested for the prototype; restrict database and log access.

## Required infrastructure

- A reachable SQL Server database using SQL authentication. Windows integrated
  authentication to the developer PC cannot work from Railway. PostgreSQL cannot
  be substituted: the migrations, rowversion and application locks use SQL Server.
- A persistent service volume mounted at `/data` for encryption keys. Losing these
  keys makes encrypted phones and saved POS responses unreadable. Back up both the
  key volume and database. Use one application instance with this volume.
- A Railway HTTPS domain. Set PublicWebUrl to this domain so printed claim links work.

## Service variables

Set secrets in Railway variables, never in source control:

| Variable | Value |
|---|---|
| ASPNETCORE_ENVIRONMENT | Production |
| ConnectionStrings__Default | SQL Server connection string with database, SQL username/password and encryption |
| Jwt__SigningKey | Unique random secret, at least 48 characters |
| PublicWebUrl | https://your-app.up.railway.app |
| DataProtection__KeysPath | /data/keys |
| Demo__SeedData | false |
| Demo__AllowConsoleOtp | true, explicitly for a restricted demo only |
| BootstrapAdmin__Email | Your private administrator login |
| BootstrapAdmin__Password | Unique password, at least 16 characters |

The bootstrap admin is created only when the staff table is empty. Remove bootstrap
variables after successful initialization. No default administrator password or
synthetic dataset is seeded outside Development. Existing local records are not
deleted or uploaded. EF migrations run at startup; use a fresh demo database and
back up existing hosted data before deployment.

OTP codes for this demo are available only in restricted Railway application logs.
Do not expose the database, log viewer, or credentials publicly. The application
intentionally refuses non-development startup unless console OTP demo mode is
explicitly enabled; a production SMS provider is not implemented.

## Deploy and verify

1. Provide hosted SQL Server and attach the key volume.
2. Configure the variables above and the public domain.
3. Run the official Railway CLI `railway up` from the workspace root.
4. Observe SUCCESS for that deployment, then check `/health`, `/admin/login`,
   administrator sign-in, customer OTP, a merchant approval and a paper receipt claim.

Builds verified locally: .NET Release build and Vite production build.
The Docker build requires a running Docker engine or Railway's build service.
Never enable synthetic seeding in Production or reuse the local demo password.
