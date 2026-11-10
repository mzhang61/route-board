# RouteBoard contributor guidelines

- Prefer readable code, descriptive names, small methods, and explicit control flow.
- Keep architecture simple. Introduce an abstraction only when it solves a demonstrated problem.
- Write source comments, documentation, and commit messages in English.
- Explain why in comments; avoid repeating what the code already says.
- Preserve existing work and applied migrations. Add new migrations for schema changes.
- Never commit secrets or print credentials in test evidence.
- Label vehicle positions as synthetic. External traffic is reference data, not real vehicle GPS.
- Verify each increment with relevant builds and tests. Record failures and unavailable checks honestly.
- Maintain architecture and oral-defense notes alongside implemented features.
- At completed milestones, record relevant verification, use English commit messages, and push to the verified origin as requested by the user. Check for secrets and remote changes first; never force-push.
