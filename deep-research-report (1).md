# COPA ELITE WORLD CUP 2026 Tournament Platform MVP on .NET 8 and Azure Free Resources

## Product goals and reality checks

Your feature list fits a strong, data driven MVP, but a few items hide most of the complexity, and cost risk.

The biggest hidden scope is “live player statistics” for 32 teams. A “live” feed typically implies a licensed sports data provider, stable IDs for players and teams, rate limits, and data quality controls. None of that is guaranteed to be free, and even when an API is “free”, it often becomes unreliable under real audience traffic. The safest MVP framing is, “team pages support player lists and stats, stats can be manually curated now, later swapped to an integration behind an adapter layer”. That keeps the UI and database design aligned with your long term vision, without blocking launch on a data provider decision.

A second assumption worth challenging is the “public team registration form” requirement. If this is a 32 nation tournament, the teams are presumably pre selected, so public team registration either means, (a) a public “apply to participate” form for future seasons, or (b) roster submission by team managers, or (c) fan registration, mislabeled. If you implement the wrong one, you build the wrong workflow. The MVP can cover all three by modeling it as “applications”, a generic submission type with a status, and an admin approval flow, then you can rename it in the UI later.

On free Azure resources, you can absolutely ship an MVP, but you should accept that free plans are intentionally limited and are labeled for learning or personal projects. For example, the App Service Free plan is explicitly intended for trials and learning, with no SLA, and production use is “not supported” as a workload target. citeturn1search0 Likewise, Azure Static Web Apps Free is positioned for hobbies and personal projects, has a hard bandwidth quota, and if you exceed free quotas the site may stop being served. citeturn0search3turn0search6

Your student subscription helps, but you must plan around subscription compatibility and quotas. Azure for Students includes USD 100 credit, no credit card at sign up, plus free monthly amounts of services. citeturn0search1turn0search5

## Azure first architecture that stays inside free tiers

This section gives you two workable architectures, then a recommendation for your constraints, C# .NET 8, “fans can register”, admin CMS feel, and free tier preference.

### Recommended MVP architecture, App Service plus Azure SQL plus External ID

**Why this is the best fit:** it gives you a normal “full stack” ASP.NET Core app experience, including server rendered pages, admin CRUD, and a clean path to robust customer authentication, without paying for Azure Static Web Apps Standard. The compute cost can be zero (App Service Free), the database can be zero (Azure SQL free offer if eligible), and customer identity can be zero (External ID core free tier), as long as you stay within quotas.

Core components:

- **Web app hosting:** Azure App Service Free plan (F1), for trials and learning. It’s shared compute, 60 CPU minutes per day, 1 GB RAM, 1 GB storage. citeturn1search0  
- **Relational database:** Azure SQL Database free offer, up to 10 databases per subscription, each includes 100,000 vCore seconds per month, 32 GB data storage and 32 GB backup storage per month, lifetime of the subscription. citeturn1search10turn16search0turn1search7  
- **Customer authentication:** Microsoft Entra External ID, core offering is free for the first 50,000 monthly active users (MAU). citeturn9search2  
- **Customer sign up methods:** External tenants support email with password, email one time passcode, and social providers like Google, Facebook, Apple, configured via user flows. citeturn10search1turn10search3  
- **Background jobs (optional for MVP):** Azure Functions Consumption plan, includes a monthly free grant of 1 million executions and 400,000 GB seconds, note that the required storage account is billed separately. citeturn0search0  
- **Static assets:** Blob Storage free amount for 12 months includes 5 GB LRS hot block plus operation quotas. citeturn1search4turn1search8  

Critical caveat you must check early: the Azure SQL free offer doc notes that the “Azure for Students Starter” offer is incompatible with the Azure SQL Database free offer, and suggests alternatives. citeturn16search0 If you are on full Azure for Students (not Starter) you might be fine, if you are on Starter, you may need to rely on credits or choose Cosmos DB free tier.

### Alternative architecture, Static Web Apps Free plus Functions plus Cosmos DB Free Tier

This option is excellent for a “public site with light personalization”, but it clashes with your “fans can register” requirement, unless you accept a strict limitation on login options.

Core components and constraints:

- Azure Static Web Apps Free includes 100 GB bandwidth per subscription, 250 MB per environment size limit, 500 MB total storage across environments, up to 10 apps, and only limited role assignment capability in Free. citeturn0search6turn0search3turn0search8  
- Static Web Apps authentication with no extra configuration supports GitHub and Microsoft Entra ID, across all plans. citeturn8search3  
- Custom authentication for Static Web Apps is only available in the Standard plan. citeturn8search2turn1search3  

That last point is the blocker. If “fans can register” means email and password sign up with a branded flow, Static Web Apps Free will not meet it.

If you can accept “sign in with GitHub or Microsoft account” for MVP, then Static Web Apps Free is viable, because you can identify users via `/.auth/me` and the client principal data, and persist predictions keyed by the Static Web Apps user ID. citeturn8search0

### Why the recommended approach wins for your requirements

You asked for CMS like admin editing, secure user accounts for fans, and a mobile first polished web experience. The App Service approach gives you:

- a single ASP.NET Core app with server side forms, validation, admin routes, and EF Core migrations,
- customer identity that supports email password and email one time passcode out of the box in External ID user flows, without paying for Static Web Apps Standard, citeturn10search1turn10search3turn8search2  
- a straightforward GitHub Actions deployment path to App Service. citeturn18search0  

## Identity and security design that will not corner you later

### Avoid building on Azure AD B2C for new projects

If you were thinking, “B2C is the standard for consumer sign in”, that is now a trap for new builds. Microsoft’s documentation for Static Web Apps with Azure AD B2C states that effective May 1, 2025, Azure AD B2C is no longer available to purchase for new customers. citeturn1search18 Since today is March 3, 2026, you should treat B2C as legacy unless you are already an existing customer.

### Use External ID for customer sign in, keep app roles for admin

External ID gives you customer sign up and sign in user flows, and supports:

- Email with password sign up, email verified at sign up with a one time passcode, plus password reset flows, citeturn10search1turn10search13  
- Email one time passcode as a passwordless primary sign in method if you choose it, citeturn10search1turn10search3  
- Optional MFA policies, external tenants support email one time passcode MFA, and SMS based MFA as an add on. citeturn10search0turn10search7  

For admin access, do not rely on hard coded emails or a “secret admin URL”. Instead, use one of these two patterns:

- **App roles in your Entra app registration**, then authorize admin routes by role claims, this scales cleanly when you add more admins and moderators. The Microsoft identity platform tutorials for ASP.NET Core cover how to configure an app for authentication and add authorization elements, and they apply to external tenants as well. citeturn11search0turn11search1  
- **Admin list in your SQL database**, where you store External ID subject identifiers that you manually approve, this is easier to reason about, but you must be careful about impersonation and audit logging.

External ID pricing is MAU based, and the core offering is free for the first 50,000 MAU, which is enough for a serious MVP. citeturn9search2 This is a much better cost story than hosting and securing your own password database when you are trying to stay on free tiers.

### Secure configuration and secrets early, even in MVP

The instinct to hard code connection strings during MVP is common, and it is also how MVPs get breached. Microsoft’s guidance across ASP.NET identity related content emphasizes not storing sensitive data in plain text configuration, and points to Key Vault based approaches for production. citeturn7search1turn13search2

A pragmatic MVP plan is:

- use App Service application settings for non secret configuration,
- use Key Vault for secrets as soon as you deploy publicly,
- rely on managed identity and DefaultAzureCredential patterns where possible. The Key Vault reference tutorial shows the pattern using DefaultAzureCredential to access Key Vault references through App Configuration. citeturn13search2

Key Vault has free transaction amounts for 12 months in the free services list, which often covers early stage usage if you are disciplined. citeturn1search4

## Data model and tournament workflow design

This section focuses on correctness and maintainability. A tournament site is not “hard” by scale, it is “hard” by edge cases and auditability, you must be able to answer “why did the table change” with confidence.

### Core domain entities

A relational model in Azure SQL is a strong fit because your data is naturally relational, teams, groups, matches, standings, predictions, users. A clean schema also makes admin tooling easier.

Recommended tables, described conceptually:

- **Tournaments**: name, season, ruleset version, status (draft, live, completed).
- **Teams**: team ID, display name, slug, flag URL, federation info, and optionally “verified” status.
- **Players**: player ID, team ID, external provider IDs (nullable), display name, position, jersey number.
- **Stages**: group stage, knockout stage.
- **Groups**: group name (A to H), tournament ID.
- **Matches**: stage, group ID nullable, home team, away team, kickoff time, venue optional, status (scheduled, live, final), and a “locked at kickoff” boolean for predictions.
- **MatchResults**: match ID, home goals, away goals, extra time, penalties, plus an “entered by admin”, “entered at”, and “revision number”.
- **StandingsSnapshots**: group ID, computed rows (or a normalized table per team per group), computed at, inputs hash, for audit and fast reads.
- **Predictions**: match ID, user ID, predicted score, submitted at, scoring awarded.
- **RegistrationSubmissions**: generic form submissions, type (team application, roster submission, contact), payload, status, reviewed by, reviewed at.

Two design choices pay off immediately:

- **Make standings a computed view, not user edited data.** Admins should only enter results and fixtures, standings should be derived. This prevents “table drift”.
- **Store a result revision history.** If an admin corrects a score, you want a trace, and you want to recompute standings from a known event log.

### Group stage computation approach

Compute group standings from match results using a deterministic rule engine:

- points, goal difference, goals for, head to head, fair play, whatever you adopt,
- a tie breaker priority list stored as tournament configuration, so you can change rules without rewriting core logic.

When an admin saves a match result, do a single transaction:

1. Upsert MatchResults with a new revision.
2. Recompute standings for that group.
3. Persist a new StandingsSnapshot.
4. If this was the final match needed to complete the group stage, trigger knockout bracket filling.

This transactional approach avoids race conditions and makes it easier to test.

### Knockout bracket population

“Dynamic bracket” usually means, “the bracket structure is fixed, you fill in teams once group rankings are final.” In real tournaments, the mapping is predetermined, for example Group A winner vs Group B runner up, and so on. You should decide the bracket mapping at the start of the project, because it affects URL structure, admin screens, and how you validate that group stage is “complete”.

A robust design is to store **BracketSlots**:

- slot code, for example R16 1 Home, R16 1 Away,
- source rule, for example “Group A 1st”,
- resolved team ID, nullable until resolved.

Then bracket resolution is simply populating slots when the source rules become resolvable.

### Predictions and locking rules

Predictions get messy when you do not lock them correctly. Decide and implement:

- last submission time is kickoff time minus a buffer, or exactly kickoff time,
- whether users can edit predictions until lock,
- scoring rules, classic is 3 points exact score, 1 point correct outcome, 0 otherwise.

Most importantly, store the “lock timestamp” per match, do not rely on “now” calculations scattered across UI and API.

## Implementation blueprint in C# .NET 8

### Front end and admin UI strategy

For a clean and minimal mobile first UI, you want server side rendering for fast first paint, and selective interactivity for bracket and live refresh. In .NET 8, Blazor Web App gives you SSR plus interactive render modes, and can scaffold authentication experiences using ASP.NET Core Identity implemented in Blazor components when you choose that route. citeturn2search5

If you adopt External ID, you will implement authentication via Microsoft.Identity.Web in your ASP.NET Core pipeline, similar to Microsoft’s External ID tutorial series for ASP.NET Core web apps. citeturn11search0turn11search1turn11search5

To keep the design aligned with “ESPN or FIFA but uncluttered”, build a design system early:

- spacing scale, typography scale, color tokens,
- table component that collapses to cards on small screens,
- bracket component that supports horizontal scroll on mobile.

image_group{"layout":"carousel","aspect_ratio":"16:9","query":["minimal sports standings table mobile design","football tournament bracket web UI mobile","clean sports scoreboard UI web"] ,"num_per_query":1}

### Project structure that scales without becoming heavy

A maintainable .NET 8 solution for this platform typically uses:

- **Web** project, Blazor or MVC UI, controllers or endpoints, auth, DI wiring.
- **Application** project, use cases, commands, queries, validation, orchestration.
- **Domain** project, entities, value objects, rules engine for standings.
- **Infrastructure** project, EF Core DbContext, repositories, External ID integration helpers, background job adapters.

This separation lets you test tournament logic without a web server.

### Database setup with the Azure SQL free offer

The Azure SQL free offer is generous, but serverless compute can be consumed faster than people expect if your database never pauses. The pricing guidance explains that serverless billing is per second based on CPU and memory used, with minimum compute billed while the database is online, and compute cost is zero only when paused. citeturn15search5turn14search7turn15search0

Practical guidance for your MVP:

- configure your app to open SQL connections late and close them fast,
- avoid background polling that keeps the database online,
- use caching for public read endpoints to reduce wake ups,
- in the free offer settings, prefer the behavior that auto pauses the database until next month if you hit the free quota, this guarantees you do not get charged, but your app will be down for the rest of the month if you exceed the free limit. citeturn16search0

Also note free offer limitations like PITR retention limited to seven days in the auto pause until next month mode, and no elastic jobs, failover groups, elastic pools. citeturn16search0

### External ID integration steps

A concrete, Microsoft documented path for External ID is:

1. Create an external tenant and user flow for sign up and sign in, choosing email with password or email one time passcode, and optionally social providers. citeturn10search3turn10search1  
2. Configure your ASP.NET Core web app for authentication using Microsoft.Identity.Web, Microsoft provides an External ID ASP.NET Core MVC sample and tutorial series for external tenants. citeturn11search5turn11search0turn11search1  
3. Later, if you need MFA, external tenants support email one time passcode MFA, and SMS as an add on. citeturn10search0turn10search7  

### Background tasks for player stats and scheduled operations

If you integrate with an external player stats provider, do not call it on every page request. Use a scheduled pull model:

- a timer triggered Azure Function to fetch updates, normalize, and write to SQL,
- the Timer trigger bindings support running on a schedule, and Microsoft recommends the isolated worker model for modern .NET versions. citeturn2search0  
- the Functions consumption plan includes a monthly free grant, but remember the storage account is billed separately. citeturn0search0  

Even if you skip live stats at MVP, this same background job mechanism is useful for:

- closing group stage automatically when all matches are final,
- generating daily prediction leaderboards,
- sending admin notifications.

### CI/CD deployment

For App Service, GitHub Actions deployment is a first class path, Microsoft documents both “Deployment Center generated workflows” and manual workflows using `azure/webapps-deploy@v3`. citeturn18search0

Microsoft also provides an end to end tutorial for deploying an ASP.NET Core 8.0 app with Azure SQL Database to App Service, including securing secrets with managed identity and Key Vault references. citeturn18search3

## Cost controls and guardrails for a free tier MVP

The goal is not just “use free services”, it is “never get surprised by charges”.

Recommended controls:

- Set a subscription budget with alert thresholds so you are notified before you burn through student credits, budgets can trigger email notifications when actual or forecasted thresholds are exceeded, resources are not stopped automatically. citeturn17search0  
- Use Azure’s guidance on avoiding charges with a free account and track your free service usage regularly, free quantities reset monthly and do not roll over. citeturn1search9  
- For Azure SQL free offer, use the “Behavior when free limit reached” setting intentionally, auto pause until next month prevents charges but can make your database inaccessible until the next calendar month, continuing for additional charges keeps the app online but can consume credits and then bill. citeturn16search0turn16search6  
- For Azure Static Web Apps Free, treat bandwidth quotas as hard limits, if you exceed the 100 GB free quota the site may not be served. citeturn0search3turn0search6  

Finally, be realistic about the App Service Free plan limitations. Microsoft positions Free and Shared plans for trials and learning, with a specific CPU minutes per day cap, and states production workloads are not supported for those tiers. citeturn1search0 That does not mean you cannot demo or even run a light MVP, it means you should plan a clean upgrade path to a paid tier before a real audience peak.

## Extensibility for multilingual support and future upgrades

You asked explicitly to architect so that languages can be added later. Do it from day one, not after launch.

At minimum:

- keep all UI strings in resource files, not inline in Razor or components,
- centralize formatting for dates, times, and numbers,
- avoid culture specific assumptions in sorting and comparisons.

.NET’s localization guidance centers on resource based localization and dependency injection through localization services. citeturn4search3turn3search0 This is compatible with a “minimalistic UI” because it is mostly a discipline and structure decision, not a design overhead.

Future upgrades that are easy if your architecture is clean:

- scale out hosting from App Service Free to Basic or higher tier when you need always on behavior and predictable performance, the App Service Free tier is not intended for production. citeturn1search0  
- move from Azure SQL free offer to a provisioned tier or elastic pool once you outgrow serverless pause behavior and free vCore seconds, serverless costs depend heavily on whether the database stays online. citeturn15search5turn14search7turn16search0  
- add real time updates for “results just posted” using Azure SignalR Service, it has a free tier with 20 concurrent connections and 20,000 messages per day. citeturn12search0turn12search4  
- if you later choose a Static Web Apps architecture for global edge hosting, be aware that custom authentication requires the Standard plan, and Free only includes preconfigured providers like GitHub and Microsoft Entra ID. citeturn8search3turn8search2turn0search3  

The overall recommendation is to ship your MVP with the App Service plus Azure SQL plus External ID architecture, because it satisfies your “fans can register” requirement without paying for Static Web Apps Standard, while keeping a clean upgrade path to stronger hosting and more automation as your audience grows. citeturn1search0turn16search0turn9search2turn10search3turn18search0