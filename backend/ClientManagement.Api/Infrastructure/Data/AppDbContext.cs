
using ClientManagement.Api.Features.Clients;
using ClientManagement.Api.Features.FollowUps;
using ClientManagement.Api.Features.Notes;
using ClientManagement.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<ClientNote> ClientNotes => Set<ClientNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Client>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Company).HasMaxLength(150);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Phone).HasMaxLength(30);

            entity.HasIndex(x => x.Name);
        });

        builder.Entity<FollowUp>()
            .HasOne(x => x.Client)
            .WithMany(x => x.FollowUps)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ClientNote>()
            .HasOne(x => x.Client)
            .WithMany(x => x.Notes)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}