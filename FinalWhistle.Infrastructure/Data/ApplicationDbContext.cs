using FinalWhistle.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<Prediction> Predictions => Set<Prediction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Team>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(e => e.Group).WithMany(g => g.Teams).HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Match>(entity =>
        {
            entity.HasOne(e => e.Tournament).WithMany(t => t.Matches).HasForeignKey(e => e.TournamentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.HomeTeam).WithMany(t => t.HomeMatches).HasForeignKey(e => e.HomeTeamId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AwayTeam).WithMany(t => t.AwayMatches).HasForeignKey(e => e.AwayTeamId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Group).WithMany(g => g.Matches).HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MatchResult>(entity =>
        {
            entity.HasOne(e => e.Match).WithOne(m => m.Result).HasForeignKey<MatchResult>(e => e.MatchId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Prediction>(entity =>
        {
            entity.HasOne(e => e.Match).WithMany(m => m.Predictions).HasForeignKey(e => e.MatchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.Predictions).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.MatchId, e.UserId }).IsUnique();
        });
    }
}
