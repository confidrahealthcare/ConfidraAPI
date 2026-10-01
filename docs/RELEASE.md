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
