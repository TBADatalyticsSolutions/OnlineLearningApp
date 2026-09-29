# TBA Datalytics Online Learning Platform

A production-oriented online learning management system built with ASP.NET Core MVC (.NET 8), Entity Framework Core, MySQL, ASP.NET Core Identity, Bootstrap, and a responsive custom design system.

## Platform capabilities
- Public course catalog and course search
- Student registration, authentication, enrollment, learning progress, and assessment history
- Instructor dashboard and role-based content management
- Administrator course, user, order, module, quiz, question, and option management
- Course/module completion tracking
- Quiz scoring with configurable pass marks
- Completion certificates and public certificate verification
- Learning resource library linking to authoritative external documentation
- Shopping cart and order recording
- CSRF protection, password policy, account lockout, role-based authorization, and secure cookies
- Idempotent course/module/quiz seed data

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
3. Restore and build:

    dotnet restore
    dotnet build

4. Create a migration whenever the EF model changes:

    dotnet ef migrations add <DescriptiveMigrationName>

5. Apply migrations locally:

    dotnet ef database update

6. Run:

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
The learning-progress and assessment features introduce additional tables/columns including:
- StudentCourses.CompletedAt
- StudentModuleProgress
- QuizAttempt
- Quiz.PassMark
- Certificate

If a local database reports an error such as Unknown column 'q.PassMark', the application code is ahead of the database schema. Generate/apply the pending EF migration before starting the application.

## Project structure
- Controllers/ — MVC request handling and authorization boundaries
- Models/ — domain entities and view models
- Data/ — EF Core context, migrations, repositories, services, seed logic, and cart
- Views/ — MVC Razor views
- wwwroot/ — CSS, JavaScript, Bootstrap assets, and learning resources
- .github/workflows/ — CI automation

## Security principles
- Never commit appsettings.json containing credentials.
- Rotate any credential that has previously appeared in public repository history.
- Keep production database credentials in the deployment platform's secret store.
- Use HTTPS in deployed environments.
- Use Identity roles for authorization; the legacy Account.Role field is retained only for schema compatibility.