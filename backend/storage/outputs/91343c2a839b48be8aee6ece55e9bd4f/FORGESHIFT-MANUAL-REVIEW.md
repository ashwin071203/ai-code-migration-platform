# Manual review required

Spring Boot source was mapped into an ASP.NET Core-shaped workspace. Review controllers, dependency injection, persistence, security, configuration, tests, and NuGet dependencies before use.

## Warnings

- Spring dependency injection and security require ASP.NET Core registration review.
- JPA/Hibernate mappings require EF Core model review.
- Maven dependencies require NuGet mapping review.
- Spring annotations require controller/service attribute mapping.
