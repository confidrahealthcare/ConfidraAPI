# Release gates

This change is reviewable code, not a production-ready compliance attestation. Public site and care application/API have separate deployment concerns.

Payments and email are disabled by default. Booking hands off to the existing external scheduling authority. No live slots, real payment capture, notifications, secure document upload or automated lab/WhatsApp integration is claimed. SQL migration requires staging validation. Persistent encrypted Data Protection keys, TLS, same-origin proxy configuration, approved retention/consent wording, professional verification/assignment operations, backups and credential rotation are required before real health-data use. Do not roll back to unauthenticated legacy endpoints.

## Test evidence

Local public build/compliance/content checks; responsive browser tests; synthetic patient/physician/guide journeys; ASP.NET authentication, authorization, consent, payment and enrolment tests. Local SQLite tests do not replace SQL Server migration and HTTPS staging tests.

## Review flags

CONTENT_REVIEW_REQUIRED: operational contacts, gestational details, public clinician roles and application hostname.
CLINICAL_REVIEW_REQUIRED: consent flow, programme suitability and review/activation workflow.
LEGAL_REVIEW_REQUIRED: new programme refund terms, privacy notices, retention and processor arrangements.

No production merge, DNS change or real transaction was performed or requested by these workflows.

## Checkout and clinical activation

The care UI now offers a Razorpay test checkout only when the authenticated availability endpoint enables it. Configure server-side `Features__Payments=true`, `Razorpay__KeyId` (test key only), `Razorpay__KeySecret`, `Razorpay__WebhookSecret`, `Payments__TermsApproved=true`, and `Payments__TermsUrl` (an approved, versioned HTTPS purchase/refund document). Keep credentials in the deployment secret store. Never use the supplied historical credential file. Live keys are deliberately rejected; live acceptance requires a separately reviewed change after provider, clinical and legal sign-off.

New orders require explicit terms acceptance. This is an entry gate, not a complete legally approved consent/contract retention system. Decide the approved terms version, retention and receipt process before enabling real purchases. Turning off new checkout by withdrawing terms approval does not prevent verification/reconciliation of existing test orders while the payment service remains configured.

The UI handles unavailable/loading, SDK failure, cancellation, failed payment, captured-payment verification and pending verification with a retry of the same order. No raw card fields, diagnosis or patient-contact prefill are sent by this application. Provider-hosted checkout still processes its own transaction information under the owner's approved processor arrangement. Configure the care-host CSP to permit only the provider origins required by the current official Razorpay integration, and verify it on HTTPS staging before enabling the feature. The public marketing host needs no checkout SDK permissions.

Assigned physicians can select a captured payment and an assessed Day 0 date. The API checks assignment, both active consent purposes, a recorded Day 0 assessment, payment ownership and capture status; repeated activation is idempotent. Eligibility reads are audited. Browser contract tests use synthetic responses; API tests exercise authorization/consent and activation separately. No actual provider transaction, refund or enrolment occurred. No database schema change was added by this checkout/activation pass.

## Operations that require owner decisions

Professional verification and patient assignment remain a restricted operator procedure. Verify clinician identity/registration and patient consent outside the public UI, use a least-privileged administrative database session, record the approver and reason in the operator audit trail, and validate the resulting role/assignment using a synthetic staging account before handling patient data. Never grant roles from a self-registration field.

SMTP reset delivery has an implementation and a disabled state; operational booking, payment and laboratory notifications need approved templates, recipient/consent rules and configured providers. Secure uploads, lab exchange and WhatsApp automation are unavailable and must not be advertised as active. Provider booking remains the external scheduling authority. Confirm its availability, cancellations and notification behaviour using owner-approved staging accounts. Reconcile any captured transaction before rollback; never retry payment to resolve an uncertain verification result.
