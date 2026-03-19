# notify — Teams Store Publishing Reference

Internal planning document. Not for public distribution.

## Protection Against Impersonation

This is worth addressing up front. The concern is a bad actor uploading a copy of `notify` to the Teams Store impersonating the real app.

**Publisher verification is the primary protection.** Microsoft requires all Teams Store publishers to verify their identity through Partner Center using a Microsoft AI Cloud Partner Program (MPN) account tied to a real organisation or individual. An unverified publisher cannot get an app into the public store — it stays as a custom/sideloaded app only.

**The app GUID is a secondary signal but not a hard lock.** The manifest `id` (`e0f04c45-7db3-417b-9d05-d02b30d675a4`) identifies this specific app package. A bad actor would generate a different GUID for their copy, meaning it would appear as a separate app in the store — not an update to ours. Admins can compare publisher name and GUID before installing.

**What to do if it happens.** Microsoft has a reporting mechanism for store impersonation — report via the app's store listing page or directly through Partner Center support. Include the verified publisher name (EvilGeniusCore) and the canonical GUID as evidence of prior art.

**Practical reality.** `notify` is a developer/admin CLI tool — not a consumer app. The attack surface for store impersonation is low. The real risk vector is someone publishing a malicious binary to GitHub impersonating the release, not a Teams Store listing.

## Prerequisites Before Submitting

These must be in place before starting the Partner Center submission.

**Legal / identity:**
- Microsoft AI Cloud Partner Program (MPN) account — free tier is sufficient
- Publisher display name decided — this appears permanently on the store listing (`EvilGeniusCore` or a variation)
- Identity verification completed in Partner Center (can take a few days)

**Public URLs — must be stable, permanent links:**
- Privacy policy URL — a real page, not a GitHub repo link. Options: GitHub Pages site, a `PRIVACY.md` served via `raw.githubusercontent.com` redirect, or a simple hosted page
- Terms of use URL — same requirement
- App landing / support page — the store listing requires a website URL. The GitHub repo page is acceptable here

**App package:**
- All manifest placeholder values replaced with real values
- Icons meet Microsoft's requirements — `color.png` must be 192×192 with no transparency, `outline.png` must be 32×32 white-on-transparent only
- Manifest version incremented for each submission
- `notify-app.zip` produced by `Package-TeamsApp.ps1`

**Functional:**
- App tested end-to-end in a real tenant
- All validation guidelines reviewed: [aka.ms/teams-store-validation](https://learn.microsoft.com/en-us/microsoftteams/platform/concepts/deploy-and-publish/appsource/prepare/teams-store-validation-guidelines)

## Submission Steps

1. Sign in to [Partner Center](https://partner.microsoft.com) with the EvilGeniusCore account
2. **Marketplace offers** → **New offer** → **Microsoft Teams app**
3. Fill in the store listing:
   - Short description (matches manifest — max 80 chars)
   - Full description (max 4000 chars) — explain what notify does, who it's for, what permissions it requires
   - Screenshots — Microsoft requires at least one screenshot of the app in use
   - Privacy policy URL
   - Terms of use URL
   - Support URL (GitHub issues page is acceptable)
4. Upload `notify-app.zip`
5. Complete the publisher attestation questionnaire (data handling, permissions, etc.)
6. Submit for review

## Review Process

- Microsoft runs automated validation first — manifest schema, icon dimensions, URL accessibility
- Manual functional review follows — a reviewer installs the app and tests it
- Review typically takes 1–2 weeks
- Microsoft will email with required changes if anything fails — common issues are privacy policy URL not resolving, icon transparency requirements, or description policy violations
- Resubmit after fixing — each resubmission restarts the review clock

## Things to Sort Before Submitting

| Item | Status | Notes |
|---|---|---|
| Partner Center account | Not started | Register at partner.microsoft.com |
| Privacy policy page | Not done | Manifest currently points to GitHub repo — needs a real URL |
| Terms of use page | Not done | Same — needs a real URL |
| GitHub repo public | Not done | Store listing links to the repo — must be public |
| Screenshots | Not done | Need at least one showing a message in Teams |
| Outline icon | Check | Must be white-on-transparent only — verify the current icon meets this |
| End-to-end testing | In progress | Required before submission |

## Publisher Brand and Domain

Publisher Verification in Entra ID requires a domain you control — Microsoft uses it to establish that the publishing entity is real. The verified domain appears on the store listing and is the primary signal admins use to trust an app.

The current `packageName` in the manifest is `com.evilgeniuscore.notify.teams`. Before submitting to the Teams Store, this should be updated to reflect the EvilGeniusLabs brand (`ca.evilgeniuslabs.notify.teams`), and Publisher Verification should be completed against `evilgeniuslabs.ca`.

This is a key reason the `evilgeniuslabs.ca` domain was acquired — it establishes a clean, dedicated identity for public-facing publishing (Teams Store, NuGet, GitHub org) separate from the personal `cmcweb.com` tenant.

**Steps when ready:**
1. Complete Publisher Verification in Entra ID against `evilgeniuslabs.ca`
2. Update `packageName` in `manifest.json.template` to `ca.evilgeniuslabs.notify.teams`
3. Update `developer.name` to `EvilGeniusLabs`
4. Update `developer.websiteUrl`, `privacyUrl`, `termsOfUseUrl` to real pages on `evilgeniuslabs.ca`
5. Increment manifest `version`

The privacy policy and terms of use pages are a hard requirement — a GitHub repo link will not pass Microsoft's automated validation. These pages need to live on `evilgeniuslabs.ca` before submission.

## Not a Priority Now

Publishing to the Teams Store is a v2+ task. The more valuable distribution path for a developer/admin tool is:

- GitHub releases with self-contained binaries
- NuGet for the `Notify.Teams` library
- Word of mouth in the DevOps/admin community

The Teams Store is worth pursuing once the tool is stable and battle-tested. A rejected submission due to bugs or policy issues can delay re-submission.
