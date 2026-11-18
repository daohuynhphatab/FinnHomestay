---
trigger: always_on
---


# Finn Homestay - Project Rules

## Project Context
- This is a university coursework project for a small homestay business named Finn Homestay.
- The website introduces the homestay, displays rooms, and supports online room booking.
- Prioritize completing the required coursework features with a working, understandable application.
- The user is a fourth-year IT student. Help them understand the implementation rather than blindly generating code.

## Mandatory Technology Constraints
- Framework: ASP.NET Core MVC.
- Project template: ASP.NET Core Web App (Model-View-Controller).
- Database: MySQL.
- Frontend: Razor Views, HTML, CSS, JavaScript, Bootstrap.
- Use Entity Framework Core with a compatible MySQL provider if the project adopts EF Core.
- Follow the actual target framework and package versions recorded in the project files.
- Never switch to ASP.NET MVC 5 (.NET Framework), ASP.NET Core Web API as the main application, React, Vue, or another frontend framework unless explicitly approved.
- Never guess framework or package versions. Inspect the project before adding dependencies.

## User Roles
1. Admin:
   - Manage user accounts.
   - Manage user roles.
2. Staff:
   - Manage room information.
   - View and manage booking orders.
3. Customer:
   - Browse and search available rooms.
   - Create room bookings.
   - View, edit, or cancel their own bookings.

## Core Features
- Public home page introducing Finn Homestay.
- Room listing and room details.
- Room search and filtering.
- User registration and login.
- Role-based authorization.
- Room CRUD: create, view, update, delete, search.
- Booking creation and management.
- Customer can only access their own bookings.
- Staff can manage rooms and bookings.
- Admin can manage accounts and roles.

## Business Rules
- Check-out date must be later than check-in date.
- Validate required fields and user input on the server.
- Prevent overlapping confirmed bookings for the same room.
- Never trust client-side authorization or validation alone.
- Protect against unauthorized access to another customer's booking.
- Use clear booking statuses and validate allowed status transitions.

## Architecture
- Use ASP.NET Core MVC with Controllers, Models, Views, and dependency injection.
- Keep business logic out of Razor Views.
- Use appropriate ViewModels for forms and page display.
- Keep database access organized and avoid duplicating query logic.
- Use asynchronous database operations where appropriate.
- Do not introduce Repository, Unit of Work, CQRS, microservices, or other patterns unless the project genuinely needs them.

## UI Requirements
- Use a consistent, friendly, welcoming homestay visual identity.
- Responsive layout for desktop and mobile.
- Provide clear navigation and understandable forms.
- Include validation messages, empty states, success messages, and error states.
- Keep admin/staff management pages practical and easy to use.

## Scope Control
- Prioritize the required coursework features before optional enhancements.
- Do not implement online payments, email automation, coupons, or external booking integrations unless explicitly requested.
- Prefer one room per booking for the initial version unless the approved requirements change.

## Working Agreement
- Before implementing a feature, identify the affected files and explain the approach.
- Make incremental changes and preserve working features.
- After changes, build and test the affected workflow.
- Explain how the user can manually verify the feature.
- If a requirement conflicts with the existing implementation, explain the conflict and ask before changing the architecture.
