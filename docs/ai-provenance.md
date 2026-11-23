# AI use log and challenged claim

## Student work

I manually designed the project direction, domain, requirements, feature priorities, and overall approach. I chose the technology stack and made the final technical and deployment decisions. I manually initiated the first Azure deployment, handled the domain correction through the registrar, used the Auth0/Azure dashboards, and personally signed in to the application. I reviewed the evidence and remain responsible for explaining and defending the submitted work.

## AI assistance

AI helped by suggesting technical options, providing code templates and drafts, explaining errors, and proposing fixes. I decided which suggestions to use. Some evidence documents identify commands, tests, scans, or captures that AI executed during user-directed work; they do not imply independent or unattended AI work. The first Azure deployment was manually initiated by me. AI did not independently initiate the original cloud deployment.

## AI-assisted D4 rollback evidence

At the student's request to complete the D4 evidence, AI assisted with a controlled staging-only rollback drill. It temporarily changed the staging release marker in the CI workflow, inspected the GitHub Actions and Azure VM journal results, then restored the original workflow in a follow-up commit. The staging VM timer performed its configured deployment and recovery. The student supplied the signed-in browser session and authorized this work. Production was not changed. The drill, cancelled workflow status, restore pipeline and observed limits are recorded in `docs/evidence/staging-rollback/README.md`.

## AI-assisted D3 evidence

At the student's request to finish remaining checklist evidence, AI suggested and added one explanatory sentence to the dispatcher form clarifying that new trips use synthetic data and appear on the public demo board. AI ran the strict frontend build, then submitted the copy change through the signed-in GitHub web editor. GitHub Actions passed `verify` and `staging`; production was not promoted. The student should review and be able to explain the change. The observed SHAs and health checks are in `docs/evidence/staging-gate/`.

## Challenged AI claim

An AI-generated local smoke check assumed a successful HTTP response would contain the HTTP/1 reason text `200 OK`. The proxy negotiated HTTP/2, where that text was absent. I challenged the check against the raw response and corrected it to inspect the numeric status. The first failure and corrected output are retained in `docs/evidence/container-deployment/`.

## Other debugging examples

- An HTTP smoke check expected the HTTP/1 text `200 OK`; the local proxy negotiated HTTP/2, so the check was corrected to parse the numeric status.
- A Linux CI test assumed PostgreSQL preserved .NET timestamp precision below a microsecond. The remote run failed; the comparison was corrected to the precision actually stored by PostgreSQL, and a regression case was retained.
- A staging SSE observer did not see `Last-Event-ID` in the ordinary browser request event. The wire-level browser event exposed the header; the original failed assertion and corrected run are retained.

## Evidence and limits

See the requirement-to-proof map and per-increment evidence folders for commands, outcomes, environment and source revisions. Original failures remain labeled as failures. The current review covers local source and build output, the 55-commit remote history, recent GitHub Actions logs, deployed API/web images, 30-day Azure runtime and database logs, screenshots, recordings, and the redacted HAR. The scan scope and detector limits are documented in `evidence/secret-review/current-cloud-review.md` and `evidence/media-review/README.md`; do not present a scoped scan as proof that every historical artifact or external service was scanned.
