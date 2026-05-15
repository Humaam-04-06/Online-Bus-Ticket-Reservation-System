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

            // ── 3. Seed Routes, PriceLists, and BusSchedules ──────
            if (!await db.Routes.AnyAsync())
            {
                var r1 = new Second_Try.Models.Route { Origin = "Lahore", Destination = "Karachi", EstimatedDurationHours = 18.5, IsActive = true };
                var r2 = new Second_Try.Models.Route { Origin = "Islamabad", Destination = "Lahore", EstimatedDurationHours = 4.5, IsActive = true };
                var r3 = new Second_Try.Models.Route { Origin = "Multan", Destination = "Islamabad", EstimatedDurationHours = 8.0, IsActive = true };
                
                db.Routes.AddRange(r1, r2, r3);
                await db.SaveChangesAsync();

                db.PriceLists.AddRange(
                    new PriceList { RouteId = r1.Id, BusType = BusType.Standard, FareAmount = 3500 },
                    new PriceList { RouteId = r1.Id, BusType = BusType.Luxury, FareAmount = 5500 },
                    new PriceList { RouteId = r2.Id, BusType = BusType.Economy, FareAmount = 1200 },
                    new PriceList { RouteId = r2.Id, BusType = BusType.Express, FareAmount = 2500 }
                );

                db.BusSchedules.AddRange(
                    new BusSchedule { RouteId = r1.Id, BusType = BusType.Standard, DepartureTime = new TimeSpan(10, 0, 0), ArrivalTime = new TimeSpan(4, 30, 0) }, // 10 AM
                    new BusSchedule { RouteId = r1.Id, BusType = BusType.Luxury, DepartureTime = new TimeSpan(22, 0, 0), ArrivalTime = new TimeSpan(16, 30, 0) }, // 10 PM
                    new BusSchedule { RouteId = r2.Id, BusType = BusType.Economy, DepartureTime = new TimeSpan(8, 0, 0), ArrivalTime = new TimeSpan(12, 30, 0) }, // 8 AM
                    new BusSchedule { RouteId = r2.Id, BusType = BusType.Express, DepartureTime = new TimeSpan(14, 0, 0), ArrivalTime = new TimeSpan(18, 30, 0) } // 2 PM
                );

                await db.SaveChangesAsync();
                Console.WriteLine("✅  [Seeder] Sample routes, prices, and schedules created.");
            }
        }
    }
}
