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

        await SeedModernCourseCatalogAsync(context);
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

    private static async Task SeedModernCourseCatalogAsync(OnlineLearningAppDbContext context)
    {
        var instructor = await context.Accounts.FirstOrDefaultAsync(a => a.Role == UserRoles.Instructor);
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
                "Professional Data Analytics with Python, SQL & Power BI",
                "Build job-ready data analytics skills using Python, SQL, Excel and Power BI. Work through data cleaning, exploratory analysis, statistical reasoning, dashboards, business insights and an end-to-end portfolio project.",
                CourseCategory.DataScience,
                65000m,
                14,
                "python-data-science.jpg",
                new[]
                {
                    ("Data Analytics Foundations & Excel", "Frame business questions, understand data types, clean spreadsheets, use formulas and pivot tables, and communicate findings clearly."),
                    ("SQL & Python for Data Analysis", "Query relational data with SQL and use Python, pandas and NumPy for cleaning, transformation, exploratory analysis and reproducible workflows."),
                    ("Statistics, EDA & Business Insights", "Apply descriptive statistics, correlation, hypothesis testing basics and exploratory visualisation to turn data into defensible insights."),
                    ("Power BI & Analytics Portfolio", "Build a professional Power BI dashboard, define KPIs, model data, publish insights and present an end-to-end portfolio project.")
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
            var course = await context.Courses.FirstOrDefaultAsync(c => c.CourseName == item.Name);

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
                await context.SaveChangesAsync();

                // CourseId is a legacy duplicate key retained for compatibility with older code.
                course.CourseId = course.Id;
                await context.SaveChangesAsync();
            }

            await EnsureCourseLearningPolicyAsync(context, course);
            await SeedModulesAndQuizAsync(context, course, item.Modules);
        }
    }

    private static async Task EnsureCourseLearningPolicyAsync(OnlineLearningAppDbContext context, Course course)
    {
        // The first course is the flagship professional GenAI pathway. Keep its learner journey explicit:
        // enrol -> study -> pay -> pass checkpoints -> submit/defend capstone -> earn certificate.
        if (!string.Equals(course.CourseName, "Generative AI & AI Agents with Python", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        course.RequirePaymentForAssessment = true;
        course.RequireAllQuizzesPassed = true;
        course.RequireCapstone = true;
        course.CapstoneTitle = "Build, Evaluate & Deploy a Production-Ready AI Agent";
        course.CapstoneDescription = "Design and implement a practical AI agent in Python that solves a clearly defined user or business problem. The project must demonstrate prompt design, structured outputs, tool use, retrieval or memory where appropriate, safety controls, evaluation, and a usable interface or API.";
        course.CapstoneRequirements = "Submit: 1) problem statement and target users, 2) architecture diagram, 3) working Python implementation or repository, 4) prompt and tool design, 5) evaluation plan with representative test cases, 6) safety/guardrail considerations, 7) results and limitations, 8) setup and usage instructions, and 9) a short reflection on what you would improve for production.";
        await context.SaveChangesAsync();

        var modules = await context.Modules
            .Where(m => m.CourseId == course.Id)
            .OrderBy(m => m.ModuleId)
            .ToListAsync();

        var enhancedContent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Python Foundations for GenAI"] = "Module objective: establish the Python foundation needed to build reliable AI applications. Topics include project environments and dependency management, functions and classes, typing and validation, configuration and secrets, API clients, JSON and structured data, exception handling, logging, reusable service functions, and basic testing. Learning activities: build a small Python client that reads configuration safely, calls a mock or real API, validates a structured response, handles failures, and records useful logs. Deliverable: a clean repository with a reproducible environment and a small command-line AI utility.",
            ["LLMs, Tools & AI Agents"] = "Module objective: understand how modern LLM applications move from a simple prompt to a controlled multi-step workflow. Topics include prompt design, system and user instructions, few-shot examples, structured outputs, tool/function calling, agent state, short-term memory, retrieval, grounding, context management, retries, observability, and human approval for risky actions. Learning activities: design a structured-output prompt, connect at least one tool, create an agent loop with explicit stopping conditions, and evaluate failure cases. Deliverable: a small tool-using agent with documented inputs, outputs, constraints, and test cases.",
            ["Build & Evaluate an AI Agent"] = "Module objective: turn the previous concepts into a practical, evaluation-driven AI system. Topics include agent architecture, guardrails, prompt-injection awareness, data privacy, tool authorization, evaluation datasets, qualitative and quantitative checks, latency/cost considerations, error handling, monitoring, deployment configuration, and responsible AI. Learning activities: define success criteria, build an evaluation set, test normal and adversarial cases, measure failures, improve the agent, and document deployment requirements. Deliverable: a production-minded agent prototype that can be presented as the course capstone foundation."
        };

        foreach (var module in modules)
        {
            if (enhancedContent.TryGetValue(module.ModuleName, out var content) &&
                (string.IsNullOrWhiteSpace(module.Content) || module.Content.Length < 400))
            {
                module.Content = content;
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedModulesAndQuizAsync(
        OnlineLearningAppDbContext context,
        Course course,
        (string Name, string Content)[] moduleSeeds)
    {
        foreach (var moduleSeed in moduleSeeds)
        {
            var module = await context.Modules.FirstOrDefaultAsync(
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
                await context.SaveChangesAsync();
            }
            else if (string.IsNullOrWhiteSpace(module.Content))
            {
                // Repair older catalog rows that were created before module content was seeded.
                module.Content = moduleSeed.Content;
                await context.SaveChangesAsync();
            }

            await SeedRecommendedMaterialAsync(context, module);

            var quizName = $"{moduleSeed.Name} Checkpoint";
            var quiz = await context.Quizzes.FirstOrDefaultAsync(
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
                await context.SaveChangesAsync();
            }

            await EnsureCheckpointQuestionsAsync(context, quiz, moduleSeed.Name);
        }
    }

    private static async Task EnsureCheckpointQuestionsAsync(
        OnlineLearningAppDbContext context,
        Quiz quiz,
        string moduleName)
    {
        var existingQuestions = await context.Questions
            .Include(q => q.Options)
            .Where(q => q.QuizId == quiz.QuizId)
            .ToListAsync();

        // Older catalog rows contained one generic placeholder question.
        // Replace that placeholder with substantive, module-aligned assessment items.
        if (existingQuestions.Count == 1 &&
            existingQuestions[0].QuestionText.StartsWith("Which outcome is central to the "))
        {
            context.Questions.Remove(existingQuestions[0]);
            await context.SaveChangesAsync();
            existingQuestions.Clear();
        }

        if (existingQuestions.Count >= 5)
        {
            return;
        }

        var questions = GetCheckpointQuestions(moduleName);
        foreach (var item in questions.Take(5))
        {
            var question = new Question
            {
                QuestionText = item.Question,
                QuestionType = "Multiple Choice",
                QuizId = quiz.QuizId
            };

            context.Questions.Add(question);
            await context.SaveChangesAsync();

            context.Options.AddRange(
                item.Options.Select((option, index) => new Option
                {
                    OptionText = option,
                    IsCorrect = index == item.CorrectIndex,
                    QuestionId = question.QuestionId
                }));

            await context.SaveChangesAsync();
        }
    }

    private static List<(string Question, string[] Options, int CorrectIndex)> GetCheckpointQuestions(string moduleName)
    {
        var name = moduleName.ToLowerInvariant();

        if (name.Contains("power bi") || name.Contains("analytics portfolio"))
            return new()
            {
                ("A sales dashboard needs a monthly revenue KPI. Which design choice makes the KPI most useful to decision-makers?",
                    new[] { "Show revenue with a clearly defined time period and comparison", "Show every transaction as a separate KPI", "Hide the date context to reduce visual clutter", "Use a decorative chart without a business measure" }, 0),
                ("In a Power BI model, what is the main purpose of defining relationships between tables?",
                    new[] { "To let filters and calculations propagate between related data", "To increase the number of dashboard colors", "To convert every column into text", "To remove the need for data validation" }, 0),
                ("Which practice most improves the credibility of an analytics portfolio project?",
                    new[] { "Documenting the business question, cleaning decisions, metrics and findings", "Showing only the final dashboard screenshot", "Removing all assumptions from the project description", "Using as many visuals as possible regardless of purpose" }, 0),
                ("A dashboard has ten charts but users cannot identify the main business problem. What should be done first?",
                    new[] { "Clarify the decision the dashboard is intended to support", "Add five more charts", "Increase every font size", "Replace all charts with pie charts" }, 0),
                ("Which measure is most appropriate for comparing profitability across products with different revenue levels?",
                    new[] { "Profit margin", "Customer name", "Order identifier", "Product description length" }, 0)
            };

        if (name.Contains("sql"))
            return new()
            {
                ("Which SQL clause filters rows before aggregation is performed?",
                    new[] { "WHERE", "HAVING", "ORDER BY", "GROUP BY" }, 0),
                ("A query uses GROUP BY customer_id and needs to keep only customers whose total spending exceeds 100000. Which clause is appropriate?",
                    new[] { "HAVING", "WHERE", "ORDER BY", "DISTINCT" }, 0),
                ("What does an INNER JOIN return when joining two tables on a matching key?",
                    new[] { "Rows with matching keys in both tables", "All rows from the left table only", "All rows from both tables regardless of matches", "Only rows with null keys" }, 0),
                ("Which SQL aggregate function returns the number of rows?",
                    new[] { "COUNT", "SUM", "AVG", "MAX" }, 0),
                ("Why should a data analyst avoid SELECT * in production reporting queries when only a few columns are needed?",
                    new[] { "It can retrieve unnecessary data and make queries less explicit", "It prevents all indexes from working", "It automatically deletes unused columns", "It converts numeric values to text" }, 0)
            };

        if (name.Contains("excel") || name.Contains("data analytics") || name.Contains("statistics") || name.Contains("eda") || name.Contains("business insights"))
            return new()
            {
                ("A business analyst finds that monthly revenue increased while the number of orders fell. What should be examined before concluding performance improved?",
                    new[] { "Average order value and the business context behind the change", "Only the number of dashboard colors", "The customer names alphabetically", "The file name of the dataset" }, 0),
                ("Which statistic describes the middle value when observations are ordered?",
                    new[] { "Median", "Variance", "Range", "Standard deviation" }, 0),
                ("What is the main purpose of a confidence interval in statistical analysis?",
                    new[] { "Describe a plausible range for a population parameter under the stated method", "Guarantee that every observation lies inside the interval", "Prove that a sample contains no bias", "Replace the need to define the population" }, 0),
                ("Why should an analyst inspect distributions before selecting a visualization or statistical method?",
                    new[] { "The distribution can reveal skewness, outliers and other features that affect interpretation", "Every dataset has the same distribution", "Distributions determine a user's password", "Visualization never depends on data characteristics" }, 0),
                ("Which Power BI practice best supports a trustworthy business insight?",
                    new[] { "Connect the insight to a defined KPI, appropriate data and a clear business question", "Choose the chart with the most decorative elements", "Hide the metric definition from viewers", "Use every available column in one visual" }, 0)
            };

        if (name.Contains("pandas") || name.Contains("data analysis") || name.Contains("data science"))
            return new()
            {
                ("In pandas, which operation is commonly used to remove rows containing missing values?",
                    new[] { "dropna()", "groupby()", "merge()", "describe()" }, 0),
                ("What is the primary purpose of exploratory data analysis?",
                    new[] { "Understand distributions, relationships, anomalies and data quality before modeling", "Guarantee that a model will be accurate", "Replace all statistical tests", "Publish a dashboard without inspecting the data" }, 0),
                ("Which pandas object is designed primarily for two-dimensional tabular data?",
                    new[] { "DataFrame", "Series", "Index", "Scalar" }, 0),
                ("Why is a train/test split useful before evaluating a predictive model?",
                    new[] { "It provides data that can help estimate performance on unseen observations", "It guarantees the model has no bias", "It removes the need for feature engineering", "It increases the number of original observations" }, 0),
                ("A numeric column contains a few extreme values. What should an analyst do first?",
                    new[] { "Investigate whether the values are valid observations or data-quality problems", "Delete every extreme value automatically", "Convert the column to text", "Replace all values with the mean without investigation" }, 0)
            };

        if (name.Contains("machine learning"))
            return new()
            {
                ("What is data leakage in machine learning?",
                    new[] { "Information unavailable at prediction time is used during training or evaluation", "The model has too few parameters", "The dataset contains only numeric features", "The model is deployed to a cloud service" }, 0),
                ("Which metric is often useful for evaluating a binary classifier when the classes are imbalanced and false positives and false negatives both matter?",
                    new[] { "F1 score", "Mean absolute error only", "R-squared only", "Training set size" }, 0),
                ("What is the main purpose of a validation set?",
                    new[] { "Compare models or tune choices before final evaluation on held-out test data", "Replace the training data", "Guarantee zero overfitting", "Increase the number of labels" }, 0),
                ("Which approach is most likely to reduce overfitting?",
                    new[] { "Regularization and appropriate validation", "Increasing model complexity without validation", "Training repeatedly on the test set", "Removing the target variable" }, 0),
                ("Why should preprocessing steps such as scaling be fitted using training data before being applied to test data?",
                    new[] { "To prevent information from the test set influencing the training process", "To make every feature categorical", "To increase the test set size", "To eliminate the target variable" }, 0)
            };

        if (name.Contains("cybersecurity") || name.Contains("secure") || name.Contains("security"))
            return new()
            {
                ("Which principle means a user should receive only the permissions required to perform their task?",
                    new[] { "Least privilege", "Open access", "Data duplication", "Fail-open design" }, 0),
                ("What is a common purpose of multi-factor authentication?",
                    new[] { "Require more than one independent authentication factor", "Replace authorization with encryption", "Make passwords public", "Disable account monitoring" }, 0),
                ("Which practice best protects an application secret such as a database password?",
                    new[] { "Store it in a secure secret-management mechanism rather than source control", "Commit it to a public repository", "Place it in a client-side JavaScript file", "Print it in application logs" }, 0),
                ("What is SQL injection?",
                    new[] { "Manipulating application SQL through unsafe handling of untrusted input", "Encrypting a database backup", "Compressing a SQL query", "Creating a database index" }, 0),
                ("Why is input validation important in a web application?",
                    new[] { "It helps enforce expected input constraints and reduces unsafe or malformed input", "It guarantees every user is trustworthy", "It removes the need for authorization", "It makes passwords unnecessary" }, 0)
            };

        if (name.Contains("cloud") || name.Contains("container") || name.Contains("devops"))
            return new()
            {
                ("What is the main benefit of packaging an application and its dependencies in a container?",
                    new[] { "More consistent execution across compatible environments", "Automatic elimination of all security risks", "Permanent storage without a database", "Removal of the need for application testing" }, 0),
                ("What is the primary purpose of a CI pipeline?",
                    new[] { "Automatically build and test changes so problems are detected early", "Store production passwords in source code", "Replace version control", "Prevent developers from writing tests" }, 0),
                ("Which practice supports safer deployments?",
                    new[] { "Automated tests and a controlled release process", "Deploying untested changes directly to production", "Disabling health checks", "Removing rollback options" }, 0),
                ("What does horizontal scaling generally mean?",
                    new[] { "Adding more application instances to handle load", "Increasing only the CPU of one server", "Deleting application instances", "Changing source-code indentation" }, 0),
                ("Why are health checks useful for a deployed web service?",
                    new[] { "They provide a machine-readable indication of service availability or readiness", "They replace application logs", "They guarantee zero downtime", "They automatically fix every application bug" }, 0)
            };

        if (name.Contains("github") || name.Contains("git"))
            return new()
            {
                ("What is the purpose of a Git branch?",
                    new[] { "To isolate a line of development so changes can be worked on independently", "To permanently delete the repository", "To replace commits with screenshots", "To encrypt all source code" }, 0),
                ("What is the purpose of a pull request?",
                    new[] { "To propose changes for review and integration into another branch", "To install Git", "To rename every commit", "To remove version history" }, 0),
                ("Why are automated CI checks valuable on a pull request?",
                    new[] { "They can detect build or test failures before changes are merged", "They guarantee the code has no security vulnerabilities", "They remove the need for code review", "They prevent all merge conflicts" }, 0),
                ("What should a good commit message communicate?",
                    new[] { "The meaningful change introduced by the commit", "The developer's private password", "The entire repository contents", "A random identifier with no context" }, 0),
                ("When using an AI coding assistant, who should remain responsible for reviewing and validating the generated code?",
                    new[] { "The developer or team responsible for the software", "The AI tool itself", "The operating system", "The Git hosting provider" }, 0)
            };

        if (name.Contains("react"))
            return new()
            {
                ("What is a React component primarily used for?",
                    new[] { "Encapsulating reusable UI structure and behavior", "Creating database tables directly", "Replacing HTTP entirely", "Encrypting browser storage" }, 0),
                ("Why is state used in a React component?",
                    new[] { "To represent data that can change and affect what the component renders", "To permanently store server backups", "To replace all CSS", "To disable re-rendering" }, 0),
                ("What is a common reason to split a large React interface into smaller components?",
                    new[] { "Improved reuse, maintainability and separation of concerns", "To make every page require a database", "To prevent any component from receiving data", "To remove the need for testing" }, 0),
                ("When a React application calls a backend API, what does the frontend typically receive?",
                    new[] { "A response containing data or an error status", "A compiled database server", "A Git branch", "A browser extension automatically" }, 0),
                ("Why are stable keys useful when rendering lists in React?",
                    new[] { "They help React identify list items across renders", "They encrypt each list item", "They create SQL indexes", "They prevent all state changes" }, 0)
            };

        if (name.Contains("python coding for kids") || name.Contains("creative programming"))
            return new()
            {
                ("A Python program asks a learner for their name and then displays a greeting. Which concept is being used to receive the learner's response?",
                    new[] { "Input", "Inheritance", "Exception handling", "Database indexing" }, 0),
                ("Which Python structure is best for repeating an action a known number of times?",
                    new[] { "A for loop", "A database table", "An HTTP header", "A CSS selector" }, 0),
                ("What is a variable useful for in a Python program?",
                    new[] { "Giving a name to a value that the program can use", "Connecting a computer to Wi-Fi", "Creating a database server automatically", "Encrypting the entire program" }, 0),
                ("A game checks whether a player's score is greater than 100 before awarding a badge. Which programming concept is most directly involved?",
                    new[] { "A conditional statement", "A package manager", "A database migration", "A network protocol" }, 0),
                ("Why is testing a small game after adding a new feature useful?",
                    new[] { "It helps discover whether the new change behaves as intended and did not introduce obvious problems", "It guarantees the game can never contain a bug", "It removes the need for source code", "It automatically publishes the game to every device" }, 0)
            };

        if (name.Contains("api") || name.Contains("fastapi") || name.Contains("python"))
            return new()
            {
                ("Which HTTP method is conventionally used to retrieve a resource?",
                    new[] { "GET", "POST", "PATCH", "DELETE" }, 0),
                ("What is the main purpose of an API validation layer?",
                    new[] { "Check that incoming data satisfies expected rules before business processing", "Automatically approve every request", "Hide all server errors from developers", "Replace database backups" }, 0),
                ("What does an HTTP 404 response generally indicate?",
                    new[] { "The requested resource was not found", "The request succeeded and created a resource", "The server is permanently offline", "The user has authenticated successfully" }, 0),
                ("Why should API endpoints enforce authorization in addition to authentication?",
                    new[] { "Authentication identifies a caller, while authorization determines permitted actions", "Authorization replaces passwords", "Authentication automatically grants every permission", "They are identical concepts" }, 0),
                ("What is a Python virtual environment useful for?",
                    new[] { "Isolating project dependencies from other Python projects", "Encrypting source code", "Replacing version control", "Hosting a database automatically" }, 0)
            };

        if (name.Contains("rag") || name.Contains("prompt") || name.Contains("multimodal") || name.Contains("generative ai") || name.Contains("agent"))
            return new()
            {
                ("What is retrieval-augmented generation designed to do?",
                    new[] { "Retrieve relevant external information and use it to ground a generated response", "Guarantee that a model never makes an error", "Replace all databases with prompts", "Train a language model from scratch for every question" }, 0),
                ("Which prompt practice usually improves the reliability of a structured task?",
                    new[] { "State the goal, relevant context, constraints and expected output format", "Remove all context and constraints", "Ask several unrelated tasks without structure", "Hide the desired output format" }, 0),
                ("Why are embeddings useful in many RAG systems?",
                    new[] { "They represent content numerically so semantically related items can be retrieved", "They automatically verify every generated claim", "They replace application authorization", "They permanently store every model response" }, 0),
                ("What is prompt injection?",
                    new[] { "Untrusted instructions attempt to manipulate an AI system into violating intended behavior", "A technique for compressing an image", "A database indexing method", "A Git merge strategy" }, 0),
                ("Why should an AI application evaluate generated answers?",
                    new[] { "To measure quality, detect failure patterns and improve reliability", "To guarantee every response is factually perfect", "To remove the need for user feedback", "To make the model training set unnecessary" }, 0)
            };

        if (name.Contains("git") || name.Contains("programming"))
            return new()
            {
                ("What does a function provide in a programming language?",
                    new[] { "A reusable unit of behavior that can accept inputs and produce a result", "A database server", "A network cable", "A password manager" }, 0),
                ("Why are exceptions handled in application code?",
                    new[] { "To respond to expected or unexpected runtime failures in a controlled way", "To make every program error impossible", "To remove the need for testing", "To convert all variables into strings" }, 0),
                ("What is version control primarily used for?",
                    new[] { "Tracking changes to files and collaborating safely on code", "Compressing application images", "Replacing application databases", "Generating passwords" }, 0),
                ("What is a unit test intended to verify?",
                    new[] { "A small, focused piece of software behavior", "The entire internet connection", "A user's identity document", "A cloud provider's billing system" }, 0),
                ("Which practice makes code easier for another developer to maintain?",
                    new[] { "Clear naming, focused functions and useful documentation", "Long functions with hidden side effects", "Duplicating logic in every file", "Removing meaningful error handling" }, 0)
            };

        return new()
        {
            ("What is the most effective way to demonstrate mastery of a technical module?",
                new[] { "Apply the concepts to a realistic task and evaluate the result", "Memorize headings without practice", "Skip exercises and assessments", "Avoid using the concepts outside the lesson" }, 0),
            ("Why are checkpoint assessments useful in an online course?",
                new[] { "They reveal gaps in understanding while learning is still in progress", "They guarantee professional certification", "They replace all practical work", "They make feedback unnecessary" }, 0),
            ("What makes a multiple-choice distractor useful?",
                new[] { "It represents a plausible misunderstanding of the target concept", "It is obviously unrelated to the question", "It contains the correct answer in disguise", "It is intentionally nonsensical" }, 0),
            ("Why should learners review incorrect quiz answers?",
                new[] { "Review helps identify misconceptions and directs further study", "Review is only useful after a perfect score", "Incorrect answers should never be discussed", "Review makes practical projects unnecessary" }, 0),
            ("Which learning activity best checks whether a learner can transfer a concept to a new situation?",
                new[] { "A realistic scenario requiring the concept to solve a new problem", "Repeating the same definition word for word", "Reading the course title again", "Skipping the assessment" }, 0)
        };
    }

    private static async Task SeedRecommendedMaterialAsync(OnlineLearningAppDbContext context, Module module)
    {
        var resources = GetRecommendedResources(module.ModuleName);
        var existingTitles = await context.CourseMaterials
            .Where(m => m.ModuleId == module.ModuleId)
            .Select(m => m.Title)
            .ToListAsync();

        if (resources.Count == 0 || resources.All(r => existingTitles.Contains(r.Title)))
        {
            return;
        }

        var adminId = await context.Accounts
            .Where(a => a.Role == UserRoles.Admin)
            .Select(a => a.Id)
            .FirstOrDefaultAsync();

        foreach (var resource in resources)
        {
            if (existingTitles.Contains(resource.Title))
            {
                continue;
            }

            context.CourseMaterials.Add(new CourseMaterial
            {
                ModuleId = module.ModuleId,
                Title = resource.Title,
                ResourceUrl = resource.Url,
                MaterialType = "Link",
                Description = resource.Description,
                UploadedById = string.IsNullOrWhiteSpace(adminId) ? null : adminId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static List<(string Title, string Url, string Description)> GetRecommendedResources(string moduleName)
    {
        var name = moduleName.ToLowerInvariant();

        if (name.Contains("power bi") || name.Contains("analytics portfolio"))
            return new()
            {
                ("Microsoft Learn: Prepare and visualize data with Power BI", "https://learn.microsoft.com/en-us/training/paths/prepare-visualize-data-power-bi/", "Official Microsoft learning path covering data preparation, transformation and interactive Power BI reports."),
                ("Microsoft Learn: Model data with Power BI", "https://learn.microsoft.com/en-us/training/paths/model-data-power-bi/", "Official Microsoft learning path covering relationships, semantic models and analytical data modeling."),
                ("Microsoft Learn: Design effective Power BI reports", "https://learn.microsoft.com/en-us/training/modules/power-bi-effective-reports/", "Guidance for designing clear, useful and interactive Power BI reports.")
            };

        if (name.Contains("sql"))
            return new()
            {
                ("PostgreSQL SQL Tutorial", "https://www.postgresql.org/docs/current/tutorial-sql.html", "Official SQL tutorial covering tables, queries, joins, aggregates, updates and deletes."),
                ("SQLBolt Interactive Lessons", "https://sqlbolt.com/", "Interactive SQL lessons covering SELECT, filtering, joins, aggregation and database concepts."),
                ("Microsoft Learn: Query and modify data with Transact-SQL", "https://learn.microsoft.com/en-us/training/paths/get-started-querying-with-transact-sql/", "Structured Microsoft learning material for practical SQL querying and data manipulation.")
            };

        if (name.Contains("pandas") || name.Contains("data analysis") || name.Contains("data science"))
            return new()
            {
                ("pandas Getting Started", "https://pandas.pydata.org/docs/getting_started/", "Official pandas documentation for exploring, cleaning and transforming tabular data."),
                ("NumPy User Guide", "https://numpy.org/doc/stable/user/", "Official NumPy guide covering arrays, numerical operations and scientific computing foundations."),
                ("Matplotlib Tutorials", "https://matplotlib.org/stable/tutorials/index.html", "Official tutorials for creating charts and visualizations in Python.")
            };

        if (name.Contains("machine learning") || name.StartsWith("ml ") || name.Contains("models") || name.Contains("evaluation"))
            return new()
            {
                ("scikit-learn User Guide", "https://scikit-learn.org/stable/user_guide/", "Official scikit-learn guide covering supervised and unsupervised learning, model selection and evaluation."),
                ("Google Machine Learning Crash Course", "https://developers.google.com/machine-learning/crash-course", "Practical introductory lessons covering regression, classification, data preparation and model evaluation."),
                ("Microsoft Learn: Create machine learning models", "https://learn.microsoft.com/en-us/training/paths/create-machine-learn-models/", "Structured learning path covering machine learning concepts and practical model development.")
            };

        if (name.Contains("cybersecurity") || name.Contains("secure") || name.Contains("security"))
            return new()
            {
                ("OWASP Top 10", "https://top10.owasp.org/2025/", "OWASP's 2025 awareness document for the most important web application security risks."),
                ("OWASP Web Security Testing Guide", "https://owasp.org/www-project-web-security-testing-guide/", "Practical guidance for understanding and testing common web application security weaknesses."),
                ("CISA Cybersecurity Resources", "https://www.cisa.gov/topics/cyber-threats-and-advisories", "Authoritative cybersecurity resources covering threats, advisories and defensive practices.")
            };

        if (name.Contains("cloud") || name.Contains("container"))
            return new()
            {
                ("Docker Get Started", "https://docs.docker.com/get-started/", "Official Docker learning material covering containers, images and basic workflows."),
                ("Microsoft Learn: Azure Fundamentals", "https://learn.microsoft.com/en-us/training/paths/azure-fundamentals-describe-cloud-concepts/", "Cloud fundamentals covering core concepts, services, security and architecture."),
                ("Kubernetes Basics", "https://kubernetes.io/docs/tutorials/kubernetes-basics/", "Official Kubernetes tutorial introducing container orchestration and core cluster concepts.")
            };

        if (name.Contains("ci/cd") || name.Contains("github") || name.Contains("team collaboration"))
            return new()
            {
                ("GitHub Actions Quickstart", "https://docs.github.com/en/actions/get-started/quickstart", "Official GitHub guide to workflows, automation, CI and deployment pipelines."),
                ("GitHub Skills: Test with Actions", "https://github.com/skills/test-with-actions", "Hands-on exercise for adding automated testing to a GitHub repository."),
                ("GitHub Flow", "https://docs.github.com/en/get-started/using-github/github-flow", "Official GitHub guidance for branch-based collaboration, pull requests and code review.")
            };

        if (name.Contains("react"))
            return new()
            {
                ("React Quick Start", "https://react.dev/learn", "Official React guide covering components, state, events, lists and everyday React concepts."),
                ("React Learn: Thinking in React", "https://react.dev/learn/thinking-in-react", "Official guide to decomposing interfaces into components and managing application data flow."),
                ("MDN: JavaScript Guide", "https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide", "Comprehensive JavaScript reference material useful for building modern React applications.")
            };

        if (name.Contains("api") || name.Contains("fastapi") || name.Contains("python"))
            return new()
            {
                ("Python Official Tutorial", "https://docs.python.org/3/tutorial/", "Official Python tutorial covering core language concepts and practical programming foundations."),
                ("FastAPI Tutorial", "https://fastapi.tiangolo.com/tutorial/", "Official FastAPI tutorial covering API routes, validation, dependencies and application structure."),
                ("MDN: HTTP Overview", "https://developer.mozilla.org/en-US/docs/Web/HTTP/Overview", "Reference material explaining HTTP requests, responses, methods, status codes and web API fundamentals.")
            };

        if (name.Contains("rag") || name.Contains("prompt") || name.Contains("multimodal") || name.Contains("generative ai") || name.Contains("agent"))
            return new()
            {
                ("Microsoft Learn: Generative AI and AI agents", "https://learn.microsoft.com/en-us/training/modules/fundamentals-generative-ai/", "Official Microsoft Learn material covering generative AI, large language models, prompts and AI agents."),
                ("Hugging Face: NLP Course", "https://huggingface.co/learn/nlp-course/chapter1/1", "Practical course material covering transformers, datasets, tokenizers and modern NLP workflows."),
                ("OpenAI: Prompt Engineering Guide", "https://platform.openai.com/docs/guides/prompt-engineering", "Official OpenAI guidance for designing prompts that produce more consistent and useful model outputs.")
            };

        if (name.Contains("git"))
            return new()
            {
                ("GitHub Skills: Introduction to GitHub", "https://github.com/skills/introduction-to-github", "Hands-on GitHub Skills exercise covering repositories, branches, commits and pull requests."),
                ("Pro Git Book", "https://git-scm.com/book/en/v2", "Comprehensive Git reference covering repositories, branching, merging, remotes and collaboration."),
                ("GitHub Docs: About Git", "https://docs.github.com/en/get-started/learning-about-github/about-git", "Official GitHub explanation of Git concepts and distributed version control.")
            };

        return new()
        {
            ("Microsoft Learn", "https://learn.microsoft.com/training/", "Browse Microsoft's self-paced technical training library for additional study."),
            ("freeCodeCamp", "https://www.freecodecamp.org/learn/", "Free interactive technical lessons and projects across programming, data and web development."),
            ("MDN Web Docs", "https://developer.mozilla.org/en-US/", "Authoritative developer documentation and tutorials for web technologies.")
        };
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
