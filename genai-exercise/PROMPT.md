You're a Senior .NET engineer.
Generate a RESTful API for a simple Task management system.

Stack
 - .NET 10, ASP.NET Core Web API with controllers.
 - EF Core with SQLite
 - JWT bearer auth (assume a basic User model exists with Id and Email; no login endpoint is needed, tests can generate their own tokens)
 - xUnit for Tests
 - Swagger UI with Swashbuckle, with a Bearer "Authorize" button

Task model
 - Id (Guid),
   Title (can't be null, empty or whitespace, 3 to 150 characters), 
   Description (optional),
   Status (is an enum with the following values: 'Todo, Doing and Done' if a task is done it can't be updated to other status),
   DueDate(DateOnly, must be after today when the task is created or when the due date changes. Use TimeProvider to get the current date),
   UserId(owner comes from the token's "sub" claim, never from the request body),
   CreatedAt,
   UpdatedAt
 
Architecture
 - Use Clean arch with four layers Core, Application, Infrastructure and Api.
 - Core should be independent of the other layers.
 - Application depends only on Core.
 - Business rules live only in the Core entity or Application Services, never in Controllers.
 - Infrastructure should implement Core Interfaces
 - Never expose entities in the API always use request and response DTOs.
 
 
Endpoints (all require a valid JWT)
 - GET api/tasks?status= list the current user's tasks
 - GET api/tasks/{id}
 - POST /api/tasks  201 with Location header
 - PUT api/tasks/{id}
 - DELETE api/tasks/{id}  204


Development only (anonymous)
 - POST /dev/token returns a JWT for a given user id, so the API can be tested from Swagger. It must not exist outside the Development environment.
 
Security
 - A task owned by another user returns 404
 - Validation errors return 400 with ValidationProblemDetails; other errors use ProblemDetails.
 - No secrets in code: read JWT settings from configuration.

Tests
 - Unit tests for the entity rules and the service, and endpoint tests with WebApplicationFactory
  covering 401 without a token, 404 for another user's task, and one 400.

Output the full solution, file by file, then a short README with how to run it.