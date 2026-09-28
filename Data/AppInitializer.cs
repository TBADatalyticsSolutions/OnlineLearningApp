using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Models;

namespace OnlineLearningApp.Data;

public class AppInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<OnlineLearningAppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppInitializer>>();

        foreach (var roleName in new[] { UserRoles.Admin, UserRoles.Instructor, UserRoles.Student })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to create role '{roleName}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        await SeedAccountAsync(
            userManager,
            configuration["SeedAccounts:AdminPassword"],
            "admin",
            "admin@example.com",
            "Administrator",
            UserRoles.Admin,
            logger);

        await SeedAccountAsync(
            userManager,
            configuration["SeedAccounts:InstructorPassword"],
            "Instructor1",
            "instructor1@example.com",
            "Lead Instructor",
            UserRoles.Instructor,
            logger);

        await SeedAccountAsync(
            userManager,
            configuration["SeedAccounts:StudentPassword"],
            "student1",
            "student1@example.com",
            "Demo Student",
            UserRoles.Student,
            logger);

        SeedModernCourseCatalog(context);
    }

    private static async Task SeedAccountAsync(
        UserManager<Account> userManager,
        string? password,
        string userName,
        string email,
        string fullName,
        string role,
        ILogger logger)
    {
        var user = await userManager.FindByNameAsync(userName);

        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "Seed account {UserName} was not created because SeedAccounts:{Role}Password is not configured.",
                    userName,
                    role);
                return;
            }

            user = new Account
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName,
                FullName = fullName,
                Email = email,
                EmailConfirmed = true,
                Role = role
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create seed account '{userName}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign role '{role}' to '{userName}': {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!string.Equals(user.Role, role, StringComparison.Ordinal))
        {
            user.Role = role;
            await userManager.UpdateAsync(user);
        }
    }

    private static void SeedModernCourseCatalog(OnlineLearningAppDbContext context)
    {
        var instructor = context.Accounts.FirstOrDefault(a => a.Role == UserRoles.Instructor);
        if (instructor is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        var catalog = new[]
        {
            new CourseSeed(
                "Generative AI & AI Agents with Python",
                "Build practical generative AI applications and autonomous AI agents with Python, APIs, tools, structured outputs, evaluation, and production patterns.",
                CourseCategory.AI,
                75000m,
                14,
                "generative-ai-python.jpg",
                new[]
                {
                    ("Python Foundations for GenAI", "Refresh the Python patterns used in modern AI applications, including functions, classes, typing, environments, packages, and API clients."),
                    ("LLMs, Tools & AI Agents", "Understand prompts, structured outputs, tool calling, agent loops, memory, retrieval, and reliable multi-step workflows."),
                    ("Build & Evaluate an AI Agent", "Create a practical agent project, add guardrails and evaluation, and prepare it for deployment.")
                }),
            new CourseSeed(
                "Prompt Engineering, RAG & Multimodal AI",
                "Learn modern prompting, retrieval-augmented generation, embeddings, document workflows, vision, and multimodal application design.",
                CourseCategory.NaturalLanguageProcessing,
                65000m,
                12,
                "prompt-rag-multimodal.jpg",
                new[]
                {
                    ("Prompt Engineering Essentials", "Design clear prompts, structured outputs, reusable templates, few-shot examples, and evaluation criteria."),
                    ("RAG & Knowledge Systems", "Build retrieval pipelines with chunking, embeddings, vector search, citations, and grounded responses."),
                    ("Multimodal AI Applications", "Work with text, images, documents, and mixed inputs to design useful multimodal AI workflows.")
                }),
            new CourseSeed(
                "Python for Data Science & AI",
                "Master Python for data analysis, visualization, automation, and the foundations required for machine learning and AI.",
                CourseCategory.DataScience,
                55000m,
                12,
                "python-data-science.jpg",
                new[]
                {
                    ("Python Programming for Data Work", "Learn Python syntax, collections, functions, files, modules, exceptions, and reusable data-processing code."),
                    ("NumPy, Pandas & Visualization", "Clean, transform, explore, and visualize real-world datasets with the core Python data stack."),
                    ("Data Projects for AI Readiness", "Build an end-to-end data project covering preparation, exploratory analysis, feature creation, and reporting.")
                }),
            new CourseSeed(
                "Machine Learning with Python",
                "Develop practical machine learning skills from data preparation through model training, evaluation, interpretation, and deployment.",
                CourseCategory.MachineLearning,
                65000m,
                14,
                "machine-learning-python.jpg",
                new[]
                {
                    ("ML Foundations & Data Preparation", "Understand supervised learning, features, targets, train-test splits, preprocessing, and leakage prevention."),
                    ("Models, Tuning & Evaluation", "Train regression and classification models, tune hyperparameters, compare metrics, and interpret results."),
                    ("End-to-End ML Project", "Build a complete machine learning solution with a reproducible pipeline and deployment-ready structure.")
                }),
            new CourseSeed(
                "Cybersecurity Fundamentals & AI Security",
                "Learn cybersecurity foundations, secure development, identity, common threats, and emerging security concerns around AI systems.",
                CourseCategory.CyberSecurity,
                60000m,
                12,
                "cybersecurity-ai.jpg",
                new[]
                {
                    ("Cybersecurity Foundations", "Explore security principles, threat models, authentication, authorization, encryption, and security operations."),
                    ("Secure Applications & APIs", "Identify common web and API risks and apply practical secure coding, validation, secrets, and access-control practices."),
                    ("AI Security & Responsible AI", "Study prompt injection, data leakage, model abuse, AI supply-chain risks, monitoring, and defensive design.")
                }),
            new CourseSeed(
                "Cloud Computing & DevOps Foundations",
                "Learn cloud architecture, containers, CI/CD, observability, infrastructure concepts, and deployment practices for modern applications.",
                CourseCategory.CloudComputing,
                60000m,
                12,
                "cloud-devops.jpg",
                new[]
                {
                    ("Cloud & Container Foundations", "Understand cloud services, networking basics, containers, images, environment configuration, and scalable application design."),
                    ("CI/CD & Deployment", "Build automated pipelines for testing, packaging, deployment, environment management, and release workflows."),
                    ("Observability & Reliability", "Apply logging, metrics, health checks, monitoring, rollback strategies, and practical reliability patterns.")
                }),
            new CourseSeed(
                "Data Engineering for Modern AI",
                "Build reliable data pipelines and platforms that support analytics, machine learning, and AI applications.",
                CourseCategory.BigData,
                70000m,
                14,
                "data-engineering-ai.jpg",
                new[]
                {
                    ("Data Pipelines & ETL", "Design robust ingestion, transformation, validation, scheduling, and data-quality workflows."),
                    ("Warehouses, Lakes & Analytics", "Understand modern storage patterns, dimensional modeling, SQL analytics, and scalable data architecture."),
                    ("Data Platforms for AI", "Prepare datasets and pipelines for machine learning and generative AI while considering governance and lineage.")
                }),
            new CourseSeed(
                "Full-Stack Web Development with Python & React",
                "Build modern web applications with Python backends, APIs, React frontends, databases, authentication, testing, and deployment.",
                CourseCategory.WebDevelopment,
                75000m,
                16,
                "python-react-fullstack.jpg",
                new[]
                {
                    ("Python APIs & Database Applications", "Build maintainable backend services, REST APIs, database models, validation, authentication, and business logic."),
                    ("React Frontend Engineering", "Create responsive interfaces with components, state, forms, API integration, and reusable UI patterns."),
                    ("Production Full-Stack Project", "Connect frontend and backend systems, add security and testing, and prepare the application for deployment.")
                }),
            new CourseSeed(
                "Git, GitHub & AI-Assisted Software Development",
                "Learn professional Git workflows, collaboration, code review, CI practices, and responsible use of AI coding assistants.",
                CourseCategory.Programming,
                45000m,
                8,
                "git-github-ai-development.jpg",
                new[]
                {
                    ("Git & GitHub Essentials", "Master repositories, branches, commits, merges, pull requests, issues, and recovery workflows."),
                    ("Team Collaboration & CI", "Use reviews, protected branches, automated checks, release practices, and maintainable repository conventions."),
                    ("AI-Assisted Development", "Use AI coding tools for planning, implementation, testing, debugging, and documentation while maintaining developer control.")
                }),
            new CourseSeed(
                "Python Coding for Kids: Creative Programming",
                "A fun, project-based introduction to Python where young learners build games, stories, drawings, quizzes, and simple interactive programs.",
                CourseCategory.Programming,
                30000m,
                8,
                "python-coding-kids.jpg",
                new[]
                {
                    ("Python Adventures", "Learn variables, input, conditions, loops, and functions through playful mini-projects and challenges."),
                    ("Games, Stories & Creative Code", "Build text adventures, quizzes, simple games, and creative programs while learning problem-solving."),
                    ("Young Developer Project", "Plan, build, test, and present a final Python project using the skills developed throughout the course.")
                })
        };

        foreach (var item in catalog)
        {
            var course = context.Courses.FirstOrDefault(c => c.CourseName == item.Name);

            if (course is null)
            {
                course = new Course
                {
                    CourseName = item.Name,
                    Description = item.Description,
                    Category = item.Category,
                    StartDate = now,
                    EndDate = now.AddDays(item.DurationWeeks * 7),
                    Price = item.Price,
                    ImageURL = item.ImageUrl,
                    Status = now < now.AddDays(item.DurationWeeks * 7) ? CourseStatus.Ongoing : CourseStatus.Completed,
                    InstructorId = instructor.Id
                };

                context.Courses.Add(course);
                context.SaveChanges();

                // CourseId is a legacy duplicate key retained for compatibility with older code.
                course.CourseId = course.Id;
                context.SaveChanges();
            }

            SeedModulesAndQuiz(context, course, item.Modules);
        }
    }

    private static void SeedModulesAndQuiz(
        OnlineLearningAppDbContext context,
        Course course,
        (string Name, string Content)[] moduleSeeds)
    {
        foreach (var moduleSeed in moduleSeeds)
        {
            var module = context.Modules.FirstOrDefault(
                m => m.CourseId == course.Id && m.ModuleName == moduleSeed.Name);

            if (module is null)
            {
                module = new Module
                {
                    ModuleName = moduleSeed.Name,
                    Content = moduleSeed.Content,
                    CourseId = course.Id
                };

                context.Modules.Add(module);
                context.SaveChanges();
            }

            var quizName = $"{moduleSeed.Name} Checkpoint";
            var quiz = context.Quizzes.FirstOrDefault(
                q => q.ModuleId == module.ModuleId && q.QuizName == quizName);

            if (quiz is null)
            {
                quiz = new Quiz
                {
                    QuizName = quizName,
                    Description = $"Checkpoint assessment for {moduleSeed.Name}.",
                    DateCreated = DateTime.UtcNow,
                    PassMark = 70m,
                    ModuleId = module.ModuleId
                };

                context.Quizzes.Add(quiz);
                context.SaveChanges();

                var question = new Question
                {
                    QuestionText = $"Which outcome is central to the {moduleSeed.Name} module?",
                    QuestionType = "Multiple Choice",
                    QuizId = quiz.QuizId
                };

                context.Questions.Add(question);
                context.SaveChanges();

                context.Options.AddRange(
                    new Option
                    {
                        OptionText = "Applying the module concepts in a practical project",
                        IsCorrect = true,
                        QuestionId = question.QuestionId
                    },
                    new Option
                    {
                        OptionText = "Skipping practice and evaluation",
                        IsCorrect = false,
                        QuestionId = question.QuestionId
                    },
                    new Option
                    {
                        OptionText = "Avoiding real-world examples",
                        IsCorrect = false,
                        QuestionId = question.QuestionId
                    });

                context.SaveChanges();
            }
        }
    }

    private sealed record CourseSeed(
        string Name,
        string Description,
        CourseCategory Category,
        decimal Price,
        int DurationWeeks,
        string ImageUrl,
        (string Name, string Content)[] Modules);
}
