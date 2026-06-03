# Git Branching Strategy

## 1. Purpose

This document defines the strategy for naming, creating, using, and managing Git branches within the project to ensure:

- Enhanced readability, clarity, and ease of code review.
- Clear distinction of the specific purpose of each branch.
- Seamless team collaboration, preventing conflicts and mitigating risks during the merge process.
- Alignment with industry-standard software development workflows.

## 2. General Principles

- Each branch should serve **one clear objective**.
- Branch names must reflect **the work being done**, not **the person doing it**.
- Avoid overly short, generic, or ambiguous branch names.
- Do not use spaces, special characters, or vague terms like `test`, `new`, or `temp`.
- Always branch out from `main` or a designated integration branch, depending on the project's specific workflow.

## 3. Branch Naming Conventions

Recommended structure:

```text
<type>/<scope>/<short-description>
```

Where:

- `type`: The category of the work.
- `scope`: The area of impact or domain (optional).
- `short-description`: A brief, descriptive, and hyphen-separated summary of the work.

### 3.1. Common Branch Types

- `feature/` — Development of a new feature.
- `bugfix/` — Resolution of a standard bug.
- `hotfix/` — Urgent fixes applied directly to the production environment.
- `refactor/` — Code restructuring that does not alter existing business logic or behavior.
- `docs/` — Documentation updates.
- `chore/` — Routine maintenance, refactoring project structure, or updating configurations/dependencies.
- `test/` — Adding or modifying tests.

### 3.2. Examples of Good Branch Names

- `feature/admin/create-user`
- `feature/group/member-selection`
- `bugfix/auth/login-failure`
- `hotfix/payment/null-reference`
- `refactor/backend/endpoint-structure`
- `docs/branch-strategy`
- `chore/dependency-update`

### 3.3. Examples to Avoid

- `Chau`
- `Hoang`
- `Khanh`
- `new-feature`
- `fix`
- `branch1`
- `temp123`

## 4. Detailed Branching Strategy

### 4.1. `main`

- The most stable branch in the repository.
- Only accepts code that has been reviewed and is fully ready to be deployed.
- Direct commits to `main` are strictly prohibited unless under highly exceptional circumstances.

### 4.2. Feature Branches (`feature/`)

- Used for developing new functionalities.
- Should be isolated per task or user story.
- For large-scale features, these can be broken down into smaller sub-branches, provided there is a clear team consensus.

_Examples:_

- `feature/frontend/dashboard-filter`
- `feature/backend/student-group-creation`

### 4.3. Bugfix Branches (`bugfix/`)

- Used for addressing non-urgent defects.
- Each branch should ideally resolve a single, specific bug.

_Examples:_

- `bugfix/frontend/modal-scroll`
- `bugfix/backend/duplicate-topic-create`

### 4.4. Hotfix Branches (`hotfix/`)

- Used for urgent, critical fixes on the production environment.
- Typically follows a fast-tracked review and merge process.
- Once resolved, changes must be backported (synchronized) to other relevant branches (e.g., `main`, `develop`) as needed.

_Examples:_

- `hotfix/api/jwt-expiration`

### 4.5. Documentation Branches (`docs/`)

- Dedicated to updating technical documentation, guidelines, or conventions.
- Should not be mixed with functional code changes if the documentation update is independent.

_Examples:_

- `docs/update-claude-md`
- `docs/api-spec-v2`

### 4.6. Maintenance Branches (`chore/`)

- Used for tasks that do not directly yield user-facing features.
- Includes file cleanups, directory restructuring, dependency updates, or configuration adjustments.

_Examples:_

- `chore/reorganize-docs`
- `chore/update-gitignore`

## 5. Scope Guidelines

For projects spanning multiple domains (e.g., frontend, backend, docs, infra), including a scope makes branches significantly easier to identify.

_Examples:_

- `feature/frontend/login-page`
- `feature/backend/group-management`
- `bugfix/frontend/form-validation`
- `chore/backend/cleanup-dependencies`

Scopes should be kept concise and remain consistent with the repository's directory structure.

## 6. Branch Workflow Rules

- Avoid addressing multiple, unrelated objectives in a single branch whenever possible.
- Delete the branch immediately after the task is completed and successfully merged.
- If a task demands significant changes outside its original scope, consider opening a new branch.
- Do not leave stale or inactive branches lingering in the repository.

## 7. Proposed Workflow

1. Create a new branch from `main`.
2. Name the branch according to the established conventions.
3. Commit in logical, incremental steps, ensuring each commit has a clear message.
4. Open a Pull Request (PR) / Merge Request (MR) upon completion.
5. Undergo peer review, make requested revisions, and merge.
6. Delete the branch post-merge unless it is explicitly required for future use.

## 8. Accompanying Commit Guidelines

A professional branching strategy is most effective when paired with clear, standardized commit messages. All commits within this project must strictly adhere to the following convention:

**Format:**

```text
[TEDF][<type>][<scope>]: <short summary>
```

**Where:**

- `[TEDF]`: The mandatory project prefix/identifier.
- `[<type>]`: The category of the commit (e.g., `feat`, `fix`, `docs`, `chore`, `refactor`). This should align with the branch type.
- `[<scope>]`: The specific area of the codebase being modified (e.g., `frontend`, `backend`, `api`, `auth`).
- `<short summary>`: A concise description of the changes written in the imperative mood (e.g., "add", not "added" or "adding").

_Examples of Valid Commits:_

- `[TEDF][feat][frontend]: add student group selection modal`
- `[TEDF][fix][backend]: prevent duplicate topic creation`
- `[TEDF][docs][readme]: update branch strategy instructions`
- `[TEDF][refactor][api]: simplify endpoint structure`
- `[TEDF][chore][infra]: update dependency packages`
- `[TEDF][test][backend]: add unit tests for group service`
- `[TEDF][hotfix][backend]: fix JWT expiration handling`
- `[TEDF][feat][backend]: implement project assignment logic`
- `[TEDF][fix][frontend]: resolve modal scroll issue`
- `[TEDF][Feat][Frontend+Backend]: implement real-time notifications` (if the change spans multiple scopes, list them separated by `+`)

_Recommendations:_

- Each commit should encapsulate a single, logical change.
- Never mix different types of work (e.g., a feature and a structural refactor) in a single commit.

## 9. Special Cases

### 9.1. Personal or Learning Branches

Even for academic or personal sandbox projects, it is highly recommended to adhere to professional formatting to build habits aligned with real-world workflows.

### 9.2. Experimental Branches

When rapid prototyping or experimenting is required, use:

- `spike/<short-description>`

These branches are strictly temporary and should never be merged directly into `main` without formal review and refinement.

## 10. Project Application Standards

All team members are expected to strictly adhere to the following principles:

- Prioritize branch names and commit messages that accurately reflect the **objective of the work**.
- Maintain consistent formatting across the entire project repository.
- Never name branches after individuals if the branch contains business logic or features.
- This document serves as the definitive reference standard for the project's entire Git workflow.
