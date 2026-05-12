using Microsoft.EntityFrameworkCore;
using Second_Try.Data;
using Second_Try.Models;

namespace Second_Try.Data
{
    /// <summary>
    /// Seeds default accounts on first run so the system is immediately usable.
    /// Runs only when the relevant tables are empty — safe to call on every startup.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // ── Make sure DB / migrations are up-to-date ──────────
            await db.Database.MigrateAsync();

            // ── 1. Seed Employees ─────────────────────────────────
            if (!await db.Employees.AnyAsync())
            {
                db.Employees.AddRange(
                    new Employee
                    {
                        FullName     = "Admin User",
                        Email        = "admin@src.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                        Role         = EmployeeRole.Admin,
                        IsActive     = true,
                        CreatedAt    = DateTime.UtcNow
                    },
                    new Employee
                    {
                        FullName     = "John Employee",
                        Email        = "employee@src.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                        Role         = EmployeeRole.Standard,
                        IsActive     = true,
                        CreatedAt    = DateTime.UtcNow
                    }
                );

                await db.SaveChangesAsync();
                Console.WriteLine("✅  [Seeder] Default employee accounts created.");
            }
            else
            {
                Console.WriteLine("ℹ️  [Seeder] Employees already exist — skipping.");
            }

            // ── 2. Seed sample Buses ──────────────────────────────
            if (!await db.Buses.AnyAsync())
            {
                db.Buses.AddRange(
                    new Bus
                    {
                        BusNumber = "SRC-001",
                        Type      = BusType.Economy,
                        Capacity  = 45,
                        Amenities = "AC",
                        IsActive  = true
                    },
                    new Bus
                    {
                        BusNumber = "SRC-002",
                        Type      = BusType.Standard,
                        Capacity  = 40,
                        Amenities = "AC, WiFi",
                        IsActive  = true
                    },
                    new Bus
                    {
                        BusNumber = "SRC-003",
                        Type      = BusType.Luxury,
                        Capacity  = 30,
                        Amenities = "AC, WiFi, Charging Ports, Reclining Seats",
                        IsActive  = true
                    },
                    new Bus
                    {
                        BusNumber = "SRC-004",
                        Type      = BusType.Express,
                        Capacity  = 35,
                        Amenities = "AC, WiFi, Snacks",
                        IsActive  = true
                    }
                );

                await db.SaveChangesAsync();
                Console.WriteLine("✅  [Seeder] Sample buses created.");
            }
            else
            {
                Console.WriteLine("ℹ️  [Seeder] Buses already exist — skipping.");
            }
        }
    }
}
