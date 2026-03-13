# Developer Guide: Writing Unit Tests for InsightERP

**Audience:** Developers who are new to unit testing  
**Project Stack:** .NET 9 · xUnit · Moq · C#  
**Last Updated:** 2026-03-13

---

## 1. What Is a Unit Test?

A **unit test** is a small, fast piece of code that you write to automatically verify that a specific function or method in your code behaves correctly.

Instead of:
> "I added a feature, I'll manually test it in Swagger and hope it works"

You write:
> "I added a feature. I also wrote a test that proves it works, and will run every time the code changes."

The goal is to catch bugs **before** they reach the dev server, staging, or production.

---

## 2. Where Do Tests Live in This Project?

```
tests/
└── src/
    ├── AuthService.Tests/         ← Tests for the AuthService microservice
    │   ├── Controllers/
    │   │   └── AuthControllerTests.cs
    │   └── Services/
    │       └── JwtTokenServiceTests.cs
    └── ApiGateway.Tests/          ← Tests for the API Gateway
        └── Controllers/
            └── GatewayControllerTests.cs
```

> **Rule:** Test files are in `tests/`, not inside `src/`. They mirror the same folder structure as the service they test.

---

## 3. Setting Up: Before You Write a Single Test

### 3.1 Make Sure You Can Build and Run Tests

Open a terminal in the project root and run:

```powershell
dotnet test ERP_Backend.slnx
```

You should see something like:
```
Test summary: total: 49, failed: 0, succeeded: 49, skipped: 0
```

If this works, you're ready. If not, tell the QA lead.

### 3.2 Your Test Project's NuGet Packages

Your `.csproj` test file already has these packages — you do **not** need to add them:

| Package | What it does |
|---|---|
| `xunit` | The testing framework (runs `[Fact]` methods) |
| `Moq` | Lets you create fake/mock dependencies |
| `Microsoft.NET.Test.Sdk` | Makes `dotnet test` work |
| `coverlet.collector` | Collects code coverage data |

---

## 4. Anatomy of a Test

Every test follows the same three-step pattern known as **Arrange → Act → Assert (AAA)**:

```csharp
[Fact]
public void MyMethod_WhenGivenValidInput_ReturnsExpectedOutput()
{
    // ARRANGE — set up everything you need
    var service = new MyService();
    var input   = "some value";

    // ACT — call the method you are testing
    var result = service.MyMethod(input);

    // ASSERT — verify the result is what you expect
    Assert.Equal("expected value", result);
}
```

### Breaking Down the Code

- **`[Fact]`** — Marks the method as a single test case that xUnit will run.
- **Method name** — Should be descriptive: `MethodName_WhenCondition_ExpectedResult`.
- **`Assert.Equal(expected, actual)`** — Checks that two values are equal. If they're not, the test fails.

---

## 5. Common Assert Methods You'll Use

```csharp
Assert.Equal(200, statusCode);               // Values must be equal
Assert.NotEqual("old", result);              // Values must be different
Assert.True(condition);                      // condition must be true
Assert.False(condition);                     // condition must be false
Assert.Null(value);                          // value must be null
Assert.NotNull(value);                       // value must not be null
Assert.IsType<OkObjectResult>(result);       // result must be this exact type
Assert.Contains("text", message);            // string must contain this text
Assert.Throws<Exception>(() => doThing());   // calling doThing() must throw
```

---

## 6. Testing a Controller (Step-by-Step Example)

Let's say you are writing tests for a new `ProductController`. It has a `GetById` method.

### Step 1: Create the Test File

Create a new file at:
```
tests/src/ProductService.Tests/Controllers/ProductControllerTests.cs
```

> Mirror the same folder structure used in `AuthService.Tests/Controllers/`.

### Step 2: Write the Test Class

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.Controllers;
using ProductService.Services;
using Xunit;

namespace ProductService.Tests.Controllers;

public class ProductControllerTests
{
    // ARRANGE helper — build the controller with whatever it needs
    private static ProductController BuildController()
    {
        var fakeRepo = new FakeProductRepository(); // see Section 8
        return new ProductController(fakeRepo);
    }

    [Fact]
    public void GetById_WithValidId_Returns200WithProduct()
    {
        // Arrange
        var ctrl = BuildController();

        // Act
        var result = ctrl.GetById("product-001");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public void GetById_WithInvalidId_Returns404()
    {
        // Arrange
        var ctrl = BuildController();

        // Act
        var result = ctrl.GetById("does-not-exist");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
```

---

## 7. Testing an Async Controller Method

If your controller method is `async` (returns `Task<IActionResult>`), your test must also be `async`:

```csharp
// ✅ Correct — async controller, async test
[Fact]
public async Task Register_WithValidData_Returns201()
{
    var ctrl   = BuildController();
    var result = await ctrl.Register(new RegisterRequest("alice", "alice@test.com", "P@ss1"));

    var created = Assert.IsType<ObjectResult>(result);
    Assert.Equal(201, created.StatusCode);
}
```

> **Common Mistake:** Calling an `async` method without `await` will make your test always pass, even when the logic is broken. Always use `await`.

---

## 8. Handling Database Dependencies (Fakes vs Mocks)

Your controllers talk to a database through a repository interface (e.g., `IUserRepository`, `IProductRepository`). In unit tests, **you must never connect to a real database**. Instead, you substitute the database with a fake.

There are two ways:

### Option A: Write a Fake Class (Recommended for State)

A fake is a simple in-memory implementation you write yourself. This is what `AuthControllerTests` uses:

```csharp
private class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _products = new()
    {
        new Product("product-001", "Widget", 9.99m)
    };

    public Task<Product?> FindByIdAsync(string id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(product);
    }

    public Task CreateAsync(Product product)
    {
        _products.Add(product);
        return Task.CompletedTask;
    }
}
```

**Use a Fake when:** You need the fake to remember state (e.g., an added item should be findable later).

### Option B: Use Moq (Recommended for Simple Setups)

Moq lets you create a fake on-the-fly without writing a full class. This is what `ApiGateway.Tests` uses for the logger:

```csharp
using Moq;

// Create a fake of the interface
var mockRepo = new Mock<IProductRepository>();

// Tell the fake what to return when FindByIdAsync is called
mockRepo
    .Setup(r => r.FindByIdAsync("product-001"))
    .ReturnsAsync(new Product("product-001", "Widget", 9.99m));

// Inject the fake into your controller
var ctrl = new ProductController(mockRepo.Object);
```

**Use Moq when:** Your test is simple and you only need to control what is returned for specific inputs.

---

## 9. Testing Multiple Cases with `[Theory]`

Instead of writing five nearly identical tests, use `[Theory]` with `[InlineData]`:

```csharp
// Without Theory — repetitive ❌
[Fact] public void Register_WithEmptyUsername_Returns400() { ... }
[Fact] public void Register_WithEmptyEmail_Returns400()    { ... }
[Fact] public void Register_WithEmptyPassword_Returns400() { ... }

// With Theory — clean ✅
[Theory]
[InlineData("",      "test@test.com", "P@ss1")]   // empty username
[InlineData("alice", "",              "P@ss1")]   // empty email
[InlineData("alice", "test@test.com", "")]         // empty password
public async Task Register_WithMissingRequiredField_Returns400(
    string username, string email, string password)
{
    var ctrl   = BuildController();
    var result = await ctrl.Register(new RegisterRequest(username, email, password));

    Assert.IsType<BadRequestObjectResult>(result);
}
```

xUnit runs this test once per `[InlineData]` row.

---

## 10. What Makes a Good Unit Test?

| ✅ Do | ❌ Don't |
|---|---|
| Test one specific behavior per test | Test multiple things in a single test |
| Use descriptive names (`Method_Condition_Result`) | Name tests `Test1`, `Test2` |
| Make tests independent of each other | Rely on one test's output in another |
| Use `Guid.NewGuid()` to generate unique usernames | Use hardcoded names that might conflict |
| Assert on the specific value you care about | Only assert that the result is not null |
| Test edge cases (empty strings, nulls, duplicates) | Only test the happy path |

---

## 11. Running Your Tests

### Run all tests in the solution
```powershell
dotnet test ERP_Backend.slnx
```

### Run tests for one project only
```powershell
dotnet test tests/src/AuthService.Tests/AuthService.Tests.csproj
```

### Run a single test class
```powershell
dotnet test ERP_Backend.slnx --filter "FullyQualifiedName~AuthControllerTests"
```

### Run a single test by name
```powershell
dotnet test ERP_Backend.slnx --filter "FullyQualifiedName~Register_WithAdminRole_Returns400"
```

---

## 12. Real Examples from This Project

Study these existing test files to understand the patterns:

| What it tests | File |
|---|---|
| Controller logic (login, register, auth rules) | [`AuthControllerTests.cs`](file:///c:/Users/User/Desktop/coding/projects/2026/ERP_backend/tests/src/AuthService.Tests/Controllers/AuthControllerTests.cs) |
| JWT generation and claims | [`JwtTokenServiceTests.cs`](file:///c:/Users/User/Desktop/coding/projects/2026/ERP_backend/tests/src/AuthService.Tests/Services/JwtTokenServiceTests.cs) |
| Moq usage for logger dependencies | [`GatewayControllerTests.cs`](file:///c:/Users/User/Desktop/coding/projects/2026/ERP_backend/tests/src/ApiGateway.Tests/Controllers/GatewayControllerTests.cs) |

---

## 13. Quick-Start Checklist for a New Developer

- [ ] Run `dotnet test ERP_Backend.slnx` — confirm all existing tests pass before touching anything.
- [ ] Create your test file in `tests/src/<ServiceName>.Tests/Controllers/` or `/Services/` to match your feature.
- [ ] Write one test for the **happy path** (valid inputs → expected response).
- [ ] Write tests for **error paths** (empty fields, duplicates, unauthorized access, not found).
- [ ] Run tests locally before pushing your code.
- [ ] Ensure CI passes on your pull request before requesting review from teammates.

---

## 14. Glossary

| Term | Meaning |
|---|---|
| **Unit Test** | A test that verifies a single class or method in isolation |
| **xUnit** | The testing framework used in this project |
| **`[Fact]`** | Marks a single test method |
| **`[Theory]`** | Marks a parameterized test with multiple data inputs |
| **Arrange / Act / Assert** | The three steps every test follows |
| **Fake** | A hand-written class that mimics a dependency (e.g., a database) |
| **Mock** | A Moq-generated fake that returns what you tell it to |
| **Interface** | A contract (e.g., `IUserRepository`) that allows swapping real and fake implementations |
| **CI/CD** | The automated pipeline that runs tests on every push |
