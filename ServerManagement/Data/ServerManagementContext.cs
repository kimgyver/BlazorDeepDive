using Microsoft.EntityFrameworkCore;
using ServerManagement.Models;

namespace ServerManagement.Data;

public class ServerManagementContext : DbContext
{
  public ServerManagementContext(DbContextOptions<ServerManagementContext> options) : base(options)
  {

  }

  public DbSet<Server> Servers { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Server>().HasData(
      new Server { ServerId = 1, Name = "Server1", City = "Toronto", IsOnline = true },
      new Server { ServerId = 2, Name = "Server2", City = "Toronto", IsOnline = false },
      new Server { ServerId = 3, Name = "Server3", City = "Toronto", IsOnline = true },
      new Server { ServerId = 4, Name = "Server4", City = "Toronto", IsOnline = false },
      new Server { ServerId = 5, Name = "Server5", City = "Montreal", IsOnline = true },
      new Server { ServerId = 6, Name = "Server6", City = "Montreal", IsOnline = false },
      new Server { ServerId = 7, Name = "Server7", City = "Montreal", IsOnline = true },
      new Server { ServerId = 8, Name = "Server8", City = "Ottawa", IsOnline = true },
      new Server { ServerId = 9, Name = "Server9", City = "Ottawa", IsOnline = false },
      new Server { ServerId = 10, Name = "Server10", City = "Calgary", IsOnline = true },
      new Server { ServerId = 11, Name = "Server11", City = "Calgary", IsOnline = false },
      new Server { ServerId = 12, Name = "Server12", City = "Halifax", IsOnline = true },
      new Server { ServerId = 13, Name = "Server13", City = "Halifax", IsOnline = false },
      new Server { ServerId = 14, Name = "Server14", City = "Halifax", IsOnline = true },
      new Server { ServerId = 15, Name = "Server15", City = "Halifax", IsOnline = false }
    );
  }
}
