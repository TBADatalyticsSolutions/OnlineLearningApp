# TBA Datalytics Online Learning Platform

A production-oriented online learning management system built with ASP.NET Core MVC (.NET 8), Entity Framework Core, MySQL, ASP.NET Core Identity, Bootstrap, and a responsive custom design system.

## Platform capabilities
- Public course catalog and course search
- Student registration, authentication, enrollment, learning progress, and assessment history
- Instructor dashboard and role-based content management
- Administrator course, user, order, module, quiz, question, and option management
- Course/module completion tracking
- Paid-course entitlement enforcement before graded assessments
- Paystack checkout initialization and server-side payment verification
- Configurable assessment pass marks, attempts, timing, question selection, and feedback
- Applied capstone project submission and instructor/admin review
- Completion certificates and public certificate verification
- Learning resource library linking to authoritative external documentation
- Shopping cart and payment/order history
- Custom Terms & Conditions, Privacy Policy, and Payment & Refund Policy
- CSRF protection, password policy, account lockout, role-based authorization, and secure cookies
- Idempotent course/module/quiz seed data

## Course completion policy
For courses configured with the default professional completion policy, a learner must:
1. Be enrolled in the course.
2. Complete the required modules.
3. Pass required checkpoint assessments.
4. Have a confirmed paid entitlement for the course.
5. Submit the required capstone project.
6. Receive capstone approval from an authorised reviewer.

Only after these requirements are satisfied may the platform issue a certificate. Direct certificate access also re-evaluates the completion rules.

## Payments
The application uses a payment-gateway abstraction with a Paystack implementation for NGN checkout. The secret key is read from configuration and must never be committed to source control. The server initializes transactions and verifies the transaction reference and amount before marking an order as paid. Paystack's documentation recommends server-side initialization and verification before delivering digital value.

Required production configuration:

    Paystack:SecretKey=<secret>

For Render or another deployment platform, configure this as a secret environment variable rather than committing it to a configuration file.

A manual admin `MarkPaid` action remains available for controlled reconciliation of offline/manual payments. It should be restricted to trusted finance/admin workflows in production.

## Capstone projects
Every course defaults to an applied capstone requirement. Course fields define the capstone title, description, and deliverables. Learners submit a project URL/repository and executive summary. Instructors/admins review submissions and can approve, reject, or request revisions. Approval is required before certification when `RequireCapstone` is enabled.

## Legal pages
The platform provides editable Razor pages for:
- `/Legal/Terms`
- `/Legal/Privacy`
- `/Legal/Refunds`

These are platform-specific starter policies and should be reviewed by the organisation's legal/privacy adviser before production use.

## Technology stack
- .NET 8 / ASP.NET Core MVC
- ASP.NET Core Identity
- Entity Framework Core 8
- Pomelo MySQL provider
- MySQL 8
- Bootstrap 5
- Bootstrap Icons
- jQuery validation

## Local setup
1. Clone the repository and open the project directory.
2. Provide a local DefaultConnection through appsettings.json, environment variables, or user secrets. Do not commit local or production secrets.
3. Provide `Paystack:SecretKey` if testing real checkout.
4. Restore and build:

    dotnet restore
    dotnet build

5. Create a migration whenever the EF model changes:

    dotnet ef migrations add <DescriptiveMigrationName>

6. Apply migrations locally:

    dotnet ef database update

7. Run:

    dotnet run

## Demo seed accounts
Seed accounts are created only when the corresponding password configuration exists:
- SeedAccounts:AdminPassword
- SeedAccounts:InstructorPassword
- SeedAccounts:StudentPassword

The application intentionally does not store seed passwords in source control.

## Database migration discipline
The repository uses EF Core migrations to evolve the schema. Model changes must be accompanied by a generated migration and updated model snapshot. Review generated migrations before applying them to shared or production databases. For automated production deployment, prefer a reviewed migration script or migration bundle rather than giving the runtime application unnecessary schema-modification privileges.

## Development branch workflow
Keep feature and hardening work on a dedicated branch, validate with:

    dotnet restore
    dotnet build
    dotnet ef migrations list
    dotnet ef database update
    dotnet run

Then open a pull request into main after local validation and database migration review.

## Current schema note
The current implementation adds payment and capstone fields/tables in addition to the learning-progress and assessment schema. If the local database reports an unknown-column/table error after pulling main, generate and apply the pending EF migration before starting the application.

## Project structure
- Controllers/ — MVC request handling and authorization boundaries
- Models/ — domain entities and view models
- Data/ — EF Core context, migrations, payment services, seed logic, and cart
- Views/ — MVC Razor views
- wwwroot/ — CSS, JavaScript, Bootstrap assets, and learning resources
- .github/workflows/ — CI automation

## Security principles
- Never commit appsettings.json containing credentials.
- Rotate any credential that has previously appeared in public repository history.
- Keep production database and payment credentials in the deployment platform's secret store.
- Use HTTPS in deployed environments.
- Use Identity roles for authorization; the legacy Account.Role field is retained only for schema compatibility.
