using Microsoft.EntityFrameworkCore;
using TravelAgency.Chat.Domain.Entities;

namespace TravelAgency.Chat.Infrastructure.Persistence;

public class ChatDbContext : DbContext
{
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
    }
}
