# FinalWhistle - COPA ELITE WORLD CUP 2026

## Project Overview

FinalWhistle is a 32-nation online football tournament platform for **COPA ELITE WORLD CUP 2026**. Built with ASP.NET Core 8, Entity Framework Core, and Azure SQL Database.

**Tournament Start Date:** April 1, 2026

---

## Technology Stack

- **Backend:** ASP.NET Core 8 (C# MVC)
- **Database:** Azure SQL Server / SQL Server LocalDB
- **ORM:** Entity Framework Core 8.0.11
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Razor Views, Bootstrap 5
- **Hosting:** Azure App Service (Free F1 tier)

---

## Project Structure

```
FinalWhistle/
├── FinalWhistle.Domain/          # Core entities and business logic
│   ├── Entities/                 # Tournament, Team, Player, Match, etc.
│   └── Enums/                    # MatchStage, MatchStatus, etc.
├── FinalWhistle.Application/     # Use cases and commands (future)
├── FinalWhistle.Infrastructure/  # EF Core, DbContext, data access
│   └── Data/
│       ├── ApplicationDbContext.cs
│       └── DbSeeder.cs
└── FinalWhistle/                 # Web application (MVC)
    ├── Areas/Admin/              # Admin panel
    │   ├── Controllers/
    │   └── Views/
    ├── Controllers/
    ├── Views/
    └── wwwroot/
```

---

## Database Schema

### Core Entities

**Tournaments**
- Id, Name, Season, Status (Draft/Live/Completed), CreatedAt, UpdatedAt

**Groups** (8 groups: A-H)
- Id, TournamentId, Name, DisplayOrder

**Teams** (32 teams)
- Id, Name, Slug, FlagUrl, GroupId, IsVerified, CreatedAt

**Players** (~352 players, 11 per team)
- Id, TeamId, Name, Position, JerseyNumber, Goals, Assists, YellowCards, RedCards, MinutesPlayed

**Matches** (80 total: 48 group + 32 knockout)
- Id, TournamentId, Stage, GroupId, HomeTeamId, AwayTeamId, KickoffTime, Status, IsLockedForPredictions

**MatchResults**
- Id, MatchId, HomeGoals, AwayGoals, HasExtraTime, HasPenalties, EnteredByAdminId, EnteredAt, RevisionNumber

**Predictions**
- Id, MatchId, UserId, PredictedHomeGoals, PredictedAwayGoals, SubmittedAt, PointsAwarded

**ApplicationUser** (extends IdentityUser)
- Id, Email, CreatedAt, Predictions

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server LocalDB or Azure SQL Database
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd FinalWhistle
   ```

2. **Update connection string** (if needed)
   
   Edit `FinalWhistle/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FinalWhistle;Trusted_Connection=true;MultipleActiveResultSets=true"
   }
   ```

3. **Apply migrations and seed database**
   ```bash
   dotnet ef database update --project FinalWhistle.Infrastructure --startup-project FinalWhistle
   ```
   
   Or simply run the application - it will auto-migrate and seed on startup.

4. **Run the application**
   ```bash
   cd FinalWhistle
   dotnet run
   ```

5. **Access the application**
   - Public site: `https://localhost:5001`
   - Admin panel: `https://localhost:5001/Admin/Teams`

---

## Default Credentials

### Admin Account
- **Email:** admin@finalwhistle.com
- **Password:** Admin@123

---

## Features Implemented (Phase 1)

### ✅ Completed

- [x] Clean architecture (Domain, Application, Infrastructure, Web)
- [x] Database schema with EF Core migrations
- [x] ASP.NET Core Identity authentication
- [x] Role-based authorization (Admin, Fan)
- [x] Database seeder (8 groups, admin user, tournament)
- [x] Admin panel structure
- [x] Teams CRUD (Create, Read, Update, Delete)
- [x] Account controller (Login, Register, Logout)

### 🚧 In Progress (Phase 2)

- [ ] Players CRUD with bulk CSV import
- [ ] Matches CRUD and fixture management
- [ ] Score entry system
- [ ] Standings computation engine
- [ ] Public team pages
- [ ] Registration form

### 📋 Planned (Phase 3-5)

- [ ] Group stage tables with auto-sorting
- [ ] Knockout bracket UI
- [ ] Auto-populate bracket from group results
- [ ] Fan predictions system
- [ ] Leaderboard
- [ ] Mobile responsiveness
- [ ] Multilingual support (i18n)

---

## Admin Panel Routes

| Route | Description |
|-------|-------------|
| `/Admin/Teams` | Manage 32 teams |
| `/Admin/Players` | Manage players (coming soon) |
| `/Admin/Matches` | Manage fixtures (coming soon) |
| `/Admin/Results` | Enter match scores (coming soon) |

---

## Public Routes

| Route | Description |
|-------|-------------|
| `/` | Home page |
| `/Account/Login` | User login |
| `/Account/Register` | Fan registration |
| `/Teams/{slug}` | Team page (coming soon) |
| `/Groups` | Group standings (coming soon) |
| `/Bracket` | Knockout bracket (coming soon) |

---

## Design System

### Color Palette
- **Primary:** `#0066CC` (FIFA Blue)
- **Secondary:** `#00A651` (Pitch Green)
- **Accent:** `#FFD700` (Trophy Gold)
- **Dark:** `#1A1A1A`
- **Light:** `#F5F5F5`

### Typography
- **Headings:** Inter (clean, modern)
- **Body:** System fonts (performance)

### Placeholder Assets
- **Team flags:** https://flagcdn.com (free API)
- **Player photos:** UI Avatars (initials-based)

---

## Azure Deployment (Free Tier)

### Resources Needed

1. **Azure SQL Database** - Free offer (100k vCore seconds/month)
2. **Azure App Service** - Free F1 tier (60 CPU min/day, 1GB RAM)
3. **Azure for Students** - $100 credit

### Deployment Steps

1. Create Azure SQL Database (Free tier)
2. Update connection string in Azure App Service configuration
3. Deploy via GitHub Actions or Visual Studio Publish
4. Run migrations: `dotnet ef database update`

### Budget Alerts
- Set alert at 50% of $100 credit
- Set alert at 80% of $100 credit
- Monitor Azure SQL vCore seconds daily

---

## Development Roadmap

### Week 1-2: Foundation ✅ COMPLETED
- [x] Solution structure
- [x] Database schema
- [x] Authentication
- [x] Admin panel skeleton
- [x] Teams CRUD

### Week 3-4: Core Data & CMS
- [ ] Players CRUD
- [ ] Matches CRUD
- [ ] Score entry
- [ ] Standings engine
- [ ] Public team pages

### Week 5: Tournament Workflow
- [ ] Group stage tables
- [ ] Knockout bracket
- [ ] Auto-populate logic

### Week 6: Fan Features
- [ ] Predictions system
- [ ] Leaderboard
- [ ] User dashboard

### Week 7: Polish & Launch
- [ ] Mobile responsiveness
- [ ] Performance optimization
- [ ] Documentation
- [ ] Deployment

---

## Contributing

This is a private project for COPA ELITE WORLD CUP 2026. Contact the project owner for collaboration.

---

## License

All rights reserved. © 2025 COPA ELITE WORLD CUP 2026

---

## Contact

For questions or support, contact the project administrator.