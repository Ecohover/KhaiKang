# Security Policy

## Supported versions

Security fixes are applied to the latest code on the `main` branch and to the
latest Docker Hub images. Users should deploy a current Git SHA image tag when
operating a production instance.

## Reporting a vulnerability

Do **not** open a public GitHub issue for a suspected vulnerability and do not
include credentials, access tokens, database exports, or customer data.

Use GitHub's private vulnerability reporting for this repository when it is
available: <https://github.com/Ecohover/KhaiKang/security/advisories/new>.
If private reporting is unavailable, contact the repository owner privately
through GitHub and include:

- a concise description and affected component;
- reproduction steps or a minimal proof of concept;
- the impact you observed; and
- any suggested mitigation.

We will acknowledge a valid report, investigate it privately, and coordinate a
fix before public disclosure whenever practical.

## Deployment responsibilities

- Keep `POSTGRES_PASSWORD` and all external secrets only in private environment
  configuration or a secrets manager.
- Use HTTPS for any internet-facing installation. Set
  `KHAIKANG_REQUIRE_HTTPS=true` only after HTTPS is actually in place.
- Do not expose PostgreSQL or the API container directly to the internet.
- Back up both the database volume and Data Protection key volume.
