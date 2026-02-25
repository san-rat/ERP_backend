# Contribution Guidelines

Welcome to the **Meridian** Backend repository! To keep our codebase clean, readable, and easy to maintain, please follow these standard contribution guidelines when working on the project.

## 🌱 Branching Strategy

We follow a standard feature-branch workflow. 
*   **`main`**: Production-ready code.
*   **`develop`**: Active development branch. All feature branches merge here first.

Both `main` and `develop` are protected branches and **require at least 2 Pull Request approval** before merging. Direct pushes to these branches are blocked by repository rules.

### Naming Conventions

Always create your branch off from `develop`. Your branch name **must** include the issue key (e.g., `MER-123`) from your tracker to tightly couple the code to the ticket.

**Format:** `<issue-key>-<short-description>`

*   `MER-26-add-github-actions-pipeline`
*   `MER-42-fix-jwt-auth-timeout`
*   `MER-10-setup-project-scaffold`

## 📝 Commit Messages

We write semantic commit messages so our git history is readable and easily parsed. **Every commit must start with its issue key.**

**Format:** `<issue-key> <type>(<scope>): <short description>`

**Types:**
*   `feat`: A new feature
*   `fix`: A bug fix
*   `docs`: Documentation only changes
*   `refactor`: A code change that neither fixes a bug nor adds a feature
*   `test`: Adding missing tests or correcting existing tests
*   `chore`: Changes to the build process, csproj, or tool configurations

**Example:** 
`MER-26 feat(DeliveryService): create endpoint for updating delivery status`

## 🔄 Pull Request Process

1. Create a branch off `develop`.
2. Commit your changes in logical, bite-sized increments using proper commit messages.
3. Push your branch and open a PR against the `develop` branch.
4. Ensure your code compiles locally (`dotnet build`) without warnings.
5. Request a review from at least one other team member.
6. Once approved, squash and merge your branch into `develop`.

---

### 📋 Pull Request Template

When opening a PR, please use this template for your description to help reviewers understand your work:

```markdown
## Description
<!-- Briefly describe what this PR does and why it is needed. -->

## Changes Made
<!-- List the key technical changes made in this PR. -->
- Added X
- Updated Y
- Removed Z

## How to Test
<!-- Provide simple steps on how the reviewer can test your changes locally. -->
1. Run `docker-compose up -d` for the database
2. Start the `[ServiceName]` via `dotnet run`
3. Hit the `/api/[endpoint]` endpoint via Swagger with payload: `{ ... }`
4. Expect a 200 OK response with the generated DB record.

## Related Tracking
<!-- Link to any relevant Jira tickets, Trello cards, or GitHub issues (e.g., Fixes #123) -->
```
