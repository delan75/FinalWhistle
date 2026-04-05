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
- **Frontend:** Razor Views, Bootstrap 5, Bootstrap Icons
- **Hosting:** Azure App Service (Free F1 tier)

---

## Project Structure

```
FinalWhistle/
├── FinalWhistle.Domain/          # Core entities and business logic
│   ├── Entities/                 # Tournament, Team, Player, Match, MatchResult, Prediction, ApplicationUser
│   └── Enums/                    # MatchStage, MatchStatus, PlayerPosition, TournamentStatus
├── FinalWhistle.Application/     # Application services and models
│   ├── Services/                 # StandingsService, BracketService, PredictionsService, LeaderboardService
│   └── Models/                   # TeamStanding, GroupStanding, BracketMatch, BracketViewModel, LeaderboardEntry, UserDashboardViewModel
├── FinalWhistle.Infrastructure/  # EF Core, DbContext, data access
│   └── Data/
│       ├── ApplicationDbContext.cs
│       └── DbSeeder.cs
└── FinalWhistle/                 # Web application (MVC)
    ├── Areas/Admin/              # Admin panel
    │   ├── Controllers/          # TeamsController, PlayersController, MatchesController, ResultsController
    │   └── Views/                # CRUD views for admin operations
    ├── Controllers/              # Public controllers
    │   ├── GroupsController.cs
    │   ├── TeamsController.cs
    │   ├── MatchesController.cs
    │   ├── BracketController.cs
    │   ├── PredictionsController.cs
    │   ├── LeaderboardController.cs
    │   ├── DashboardController.cs
    │   └── AccountController.cs
    ├── Views/                    # Public views
    └── wwwroot/                  # Static assets
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
- Id, MatchId, HomeGoals, AwayGoals, HasExtraTime, ExtraTimeHomeGoals, ExtraTimeAwayGoals, HasPenalties, PenaltiesHomeScore, PenaltiesAwayScore, EnteredByAdminId, EnteredAt, RevisionNumber

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

## Features Implemented

### ✅ Phase 1: Foundation - COMPLETED

- [x] Clean architecture (Domain, Application, Infrastructure, Web)
- [x] Database schema with EF Core migrations
- [x] ASP.NET Core Identity authentication
- [x] Role-based authorization (Admin, Fan)
- [x] Database seeder (8 groups, admin user, tournament)
- [x] Admin panel structure
- [x] Teams CRUD (Create, Read, Update, Delete)
- [x] Players CRUD with statistics tracking
- [x] Matches CRUD with fixture scheduling
- [x] Results entry system with revision history
- [x] Account controller (Login, Register, Logout)

### ✅ Phase 2: Public Pages & Standings - COMPLETED

- [x] **StandingsService** - Calculates group standings with FIFA ranking rules
  - Points calculation: Win = 3 pts, Draw = 1 pt, Loss = 0 pts
  - Tiebreaker rules: Points → Goal Difference → Goals For → Team Name
  - Supports single group and all groups queries
  - Auto-computes from completed matches
- [x] Public groups page (`/Groups`) - Displays all 8 group standings tables
- [x] Public teams list page (`/Teams`) - Lists all 32 teams grouped by A-H
- [x] Public team details page (`/Teams/{slug}`) - Shows squad roster and match history
- [x] Public matches page (`/Matches`) - Full fixture list with filters (Group, Stage, Team)
- [x] Navigation updated with public links

### ✅ Phase 3: Knockout Bracket - COMPLETED

- [x] **BracketService** - Automated knockout bracket management
  - `GenerateRoundOf16Async()` - Creates 8 R16 matches from group standings using FIFA matchup rules (1A vs 2B, 1C vs 2D, etc.)
  - `AdvanceWinnersAsync()` - Auto-advances winners through knockout rounds (R16 → QF → SF → Final)
  - Winner determination: Regular time → Extra time → Penalties
  - `GetBracketMatchesAsync()` - Retrieves all knockout matches
- [x] Visual bracket display (`/Bracket`) - 4-column progressive layout showing R16, QF, SF, Final
- [x] Admin controls for bracket generation and advancement
- [x] TBD team support for knockout stages
- [x] Winner highlighting and penalties display

### ✅ Phase 4: Fan Features - COMPLETED

- [x] **PredictionsService** - Fan prediction management with points calculation
  - `SubmitPredictionAsync()` - Fans predict match scores before kickoff
  - Predictions locked at kickoff time (IsLockedForPredictions)
  - Update existing predictions until match starts
  - `AwardPointsForMatchAsync()` - Auto-awards points when results entered
  - Points calculation: Exact score = 3 pts, Correct result = 1 pt, Wrong = 0 pts
  - `CalculateUserTotalPointsAsync()` - Aggregates user's total points
  - `GetUserPredictionsAsync()` - Retrieves user's prediction history
- [x] **LeaderboardService** - Global fan rankings
  - `GetTopUsersAsync()` - Returns top 100 users with statistics
  - Sorting criteria: Total Points → Exact Scores → Predictions Count
  - `GetUserRankAsync()` - Looks up individual user's rank
  - Tracks: Total Points, Predictions Count, Exact Scores, Correct Results
- [x] Predictions UI (`/Predictions`) - Make predictions for upcoming matches
- [x] Prediction history (`/Predictions/MyPredictions`) - View all predictions with points
- [x] Leaderboard page (`/Leaderboard`) - Global rankings with trophy icons for top 3
- [x] Personal dashboard (`/Dashboard`) - User stats, performance breakdown, recent predictions
- [x] Auto-award points when results entered (integrated with ResultsController)
- [x] Navigation updated with fan links (Predictions, Leaderboard, Dashboard)

---

## Admin Panel Routes

| Route | Description | Status |
|-------|-------------|--------|
| `/Admin/Teams` | Manage 32 teams | ✅ Implemented |
| `/Admin/Players` | Manage players with statistics | ✅ Implemented |
| `/Admin/Matches` | Manage fixtures and scheduling | ✅ Implemented |
| `/Admin/Results` | Enter match scores with auto-award points | ✅ Implemented |

---

## Public Routes

| Route | Description | Status |
|-------|-------------|--------|
| `/` | Home page | ✅ Implemented |
| `/Account/Login` | User login | ✅ Implemented |
| `/Account/Register` | Fan registration | ✅ Implemented |
| `/Teams` | List all 32 teams by group | ✅ Implemented |
| `/Teams/{slug}` | Team details with squad and history | ✅ Implemented |
| `/Groups` | Group standings (8 tables) | ✅ Implemented |
| `/Matches` | Fixture list with filters | ✅ Implemented |
| `/Bracket` | Knockout bracket display | ✅ Implemented |
| `/Predictions` | Make predictions (authenticated) | ✅ Implemented |
| `/Predictions/MyPredictions` | Prediction history (authenticated) | ✅ Implemented |
| `/Leaderboard` | Global fan rankings | ✅ Implemented |
| `/Dashboard` | Personal stats and performance (authenticated) | ✅ Implemented |

---

## Core Services

### StandingsService
Computes group standings from completed matches using FIFA ranking rules.

**Key Methods:**
- `GetAllGroupStandingsAsync(tournamentId)` - Returns standings for all 8 groups
- `GetGroupStandingAsync(groupId)` - Returns standings for a specific group

**Logic:**
- Iterates through completed matches in a group
- Calculates: Played, Won, Drawn, Lost, Goals For, Goals Against, Points
- Applies FIFA tiebreaker: Points → Goal Difference → Goals For → Team Name
- Returns sorted list with positions (1-4 per group)

### BracketService
Manages knockout bracket generation and winner progression.

**Key Methods:**
- `GenerateRoundOf16Async(tournamentId)` - Creates 8 R16 matches from group standings
- `AdvanceWinnersAsync(currentStage, tournamentId)` - Advances winners to next round
- `GetBracketMatchesAsync(tournamentId)` - Retrieves all knockout matches

**Logic:**
- R16 generation: Reads top 2 teams from each group, creates matches using FIFA rules (1A vs 2B, etc.)
- Winner advancement: Determines winner from regular time, extra time, or penalties
- Auto-creates next round matches with TBD teams if winners not yet determined
- Validates: All current stage matches completed before advancing

### PredictionsService
Handles fan predictions and points calculation.

**Key Methods:**
- `SubmitPredictionAsync(matchId, userId, homeGoals, awayGoals)` - Submit or update prediction
- `AwardPointsForMatchAsync(matchId)` - Calculate and award points for all predictions
- `CalculateUserTotalPointsAsync(userId)` - Sum user's total points
- `GetUserPredictionsAsync(userId)` - Retrieve user's prediction history

**Logic:**
- Validates: Match not locked, kickoff time not passed
- Updates existing prediction or creates new one
- Points calculation: Exact score (3 pts) → Correct result (1 pt) → Wrong (0 pts)
- Auto-triggered when admin enters match result

### LeaderboardService
Generates global fan rankings.

**Key Methods:**
- `GetTopUsersAsync(count)` - Returns top N users with statistics
- `GetUserRankAsync(userId)` - Looks up specific user's rank

**Logic:**
- Groups predictions by user, sums points
- Counts exact scores and correct results
- Sorts by: Total Points (desc) → Exact Scores (desc) → Predictions Count (asc)
- Returns top 100 by default

---

## Design System

### Color Palette
- **Primary:** `#0066CC` (FIFA Blue)
- **Secondary:** `#00A651` (Pitch Green)
- **Accent:** `#FFD700` (Trophy Gold)
- **Dark:** `#1A1A1A`
- **Light:** `#F5F5F5`

### Typography
- **Headings:** Clean, modern sans-serif
- **Body:** System fonts (performance optimized)

### Icons
- Bootstrap Icons CDN integrated for visual clarity

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

### Week 3-4: Core Data & CMS ✅ COMPLETED
- [x] Players CRUD
- [x] Matches CRUD
- [x] Score entry
- [x] Standings engine
- [x] Public team pages

### Week 5: Tournament Workflow ✅ COMPLETED
- [x] Group stage tables
- [x] Knockout bracket
- [x] Auto-populate logic

### Week 6: Fan Features ✅ COMPLETED
- [x] Predictions system
- [x] Leaderboard
- [x] User dashboard

### Week 7: Polish & Launch 🎯 NEXT
- [ ] Mobile responsiveness optimization
- [ ] Performance optimization (caching)
- [ ] Email notifications
- [ ] Social sharing
- [ ] Badges/achievements
- [ ] Azure deployment

---

## Git Workflow

### Branch Strategy
- **Main branch:** Production-ready code only
- **Dev branch:** Integration branch for all features
- **Feature branches:** Individual feature development (15+ branches)

### Branches Created
1. `feature/standings-service` - Standings computation engine
2. `feature/groups-standings-page` - Public groups page
3. `feature/teams-public-pages` - Public teams pages
4. `feature/matches-public-page` - Public matches page
5. `feature/bracket-service` - Bracket service
6. `feature/bracket-ui` - Bracket UI
7. `feature/predictions-service` - Predictions service
8. `feature/leaderboard-service` - Leaderboard service
9. `feature/predictions-ui` - Predictions UI
10. `feature/leaderboard-ui` - Leaderboard UI
11. `feature/dashboard-ui` - Dashboard UI
12. `feature/di-registration` - DI container setup
13. `feature/navigation-updates` - Navigation links
14. `feature/auto-award-points` - Auto-award points
15. `feature/bootstrap-icons-cdn` - Bootstrap Icons CDN

**All PRs target `dev` branch** - No direct main commits

---

## Build Status

```
Build: ✅ SUCCESS
Errors: 0
Warnings: 7 (non-critical nullable reference checks in Razor views)
Compilation Time: ~12 seconds
```

---

## Contributing

This is a private project for COPA ELITE WORLD CUP 2026. Contact the project owner for collaboration.

---

## License

All rights reserved. © 2025 COPA ELITE WORLD CUP 2026

---

## Contact

For questions or support, https://portfolio-mct.vercel.app