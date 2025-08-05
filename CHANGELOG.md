# Changelog

All notable changes to the Club Management Platform will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Coach User Role**: Added new `Coach` user role to the system
  - Extended `UserRole` enum with `Coach` option
  - Added demo coach user in database seeder (`coach@demo.localhost` / `Coach123!`)
  - Coach role can be used for trainers, class instructors, and coaching staff
  - **Coach Event Registration Support**: Coaches now get Member profiles so they can register for events as participants
  
- **Demo Member Account**: Added complete demo member user for testing
  - Demo member user with member portal access (`member@demo.localhost` / `Member123!`)
  - Complete member profile with emergency contact and medical information
  - Basic membership tier, active status

- **Enhanced Security Implementation**:
  - PBKDF2 password hashing with SHA256 and 100,000 iterations
  - Separate salt storage for enhanced security
  - Password change timestamp tracking
  - Proper JWT claims-based authentication with role information

- **Development Improvements**:
  - Added missing PWA icon (512px) to fix ServiceWorker installation
  - Relaxed CORS settings for development (accepts all origins)
  - Disabled HTTPS redirection for development (with production reminder)
  - Single migration approach for cleaner database schema

### Changed
- **Authentication System**: Completely overhauled password management
  - Moved from basic authentication to secure PBKDF2 hashing
  - Added separate salt storage instead of combined hash+salt
  - Enhanced JWT token generation with user role claims

- **Database Architecture**: Consolidated all changes into single initial migration
  - All password fields included in initial migration
  - Demo users created with proper password hashing in seeder
  - Clean single-migration approach for easier deployment

- **CORS Configuration**: Modified for development flexibility
  - Changed from specific allowed origins to `AllowAnyOrigin()`
  - Moved CORS middleware before HTTPS redirection
  - Added production reminders for re-enabling security features

### Fixed
- **ServiceWorker Installation**: Fixed PWA installation error
  - Added missing `icon-512.png` file
  - ServiceWorker now installs successfully without errors

- **CORS Headers in Redirects**: Fixed missing CORS headers in HTTP→HTTPS redirects
  - Moved CORS middleware before HTTPS redirection in pipeline
  - Redirect responses now include proper CORS headers

- **Database Migration Issues**: Resolved pending model changes
  - Regenerated initial migration to include all current model state
  - Eliminated migration synchronization issues

### Security
- **Enhanced Password Security**:
  - PBKDF2 with SHA256 hash algorithm
  - 100,000 iterations for computational security
  - 32-byte random salt generation
  - Constant-time password comparison
  - Password change timestamp tracking

## [Initial Release] - 2024-08-04

### Added
- **Multi-Tenant Architecture**: Complete schema-per-tenant isolation
- **User Management**: User accounts with role-based access control
- **Member Management**: Full member lifecycle management
- **Event Management**: Class and event scheduling system
- **Facility Management**: Dynamic facility booking system
- **Hardware Management**: Equipment tracking and assignment
- **Payment Integration**: Stripe payment processing
- **Modern UI**: Blazor WASM with MudBlazor Material Design components
- **API Architecture**: Clean architecture with Entity Framework Core
- **Authentication**: JWT-based authentication system
- **Database**: PostgreSQL with Entity Framework Core migrations
- **Infrastructure**: Docker containerization with docker-compose
- **Documentation**: Comprehensive README and setup scripts