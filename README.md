### Consignes .NET: 
* Ignorez les migrations BDD
* Ne pas modifier les classes qui ont un commentaire: `// WARN: Should not be changed during the exercise
`
* Pour lancer les tests: `dotnet test`

## Refactoring steps

### Step 1: Separate architecture into four layers

- **Domain:** product and order entities, plus the existing product eligibility rules.
- **Application:** order processing, product workflows, and persistence/notification contracts.
- **Infrastructure:** EF Core context, entity mappings, repository implementation, and the existing notification implementation.
- **API:** HTTP controller, response DTO, and dependency registration.

Moved processing out of the controller, replaced direct database access in application services with a repository contract, and moved persistence mappings out of domain entities. Added concise uppercase comments at key architectural boundaries.

Preserved the existing business conditions, stock updates, date comparisons, save locations, and notification ordering. Protected declarations were left unchanged; no new packages were added.

Validation: `dotnet test` passed before and after the refactoring (2 tests). Existing coverage is limited and does not prove that every business scenario is regression-free.

### Step 2: Cover primary business cases

Added 7 application unit tests for normal, seasonal, and expired products, checking stock, lead time, saves, notifications, and execution order. Added 3 HTTP integration tests checking response IDs, persisted stock, and expiration notifications through all four layers.

Existing tests remain unchanged. No production code, business logic, or dependencies were changed. Run all 12 tests with `dotnet test`.

### Step 3: Move product behavior into the domain

Moved eligibility rules, stock changes, and lead-time updates into `Product`, replacing the static `ProductRules` class. Application services retain workflow orchestration, saves, and notifications in their original order. Public setters, business conditions, and date comparisons remain unchanged; no validation or dependencies were added.

### Step 4: Clean code and general improvements

- Replaced abbreviated identifiers in `ProductService` and renamed `HandleExpiredProduct` to `HandleUnavailableExpirableProduct`, updating its caller.
- Aligned editable Domain, Application, and Infrastructure namespaces with their layers and updated references. Protected declarations retain their original namespaces.
- Made `Product.Available` and `Product.LeadTime` setters private, added parameterless and initialization constructors, and adapted test setup.
- Added application processing results and whole-order validation, mapped to HTTP 404 for missing orders and 400 for null items, unsupported types, null names, or missing type-specific dates. Invalid orders are rejected before any processing; direct product-service calls guard required notification values.
- Added 5 validation tests covering missing orders, unsupported types, null items, missing expiration dates, and missing names (17 tests total).

Valid-order business conditions and execution order remain unchanged. Invalid-order handling intentionally changes as described above. Run the full suite with `dotnet test`.
