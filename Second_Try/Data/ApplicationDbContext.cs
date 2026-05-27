using Microsoft.EntityFrameworkCore;
using Second_Try.Models;

namespace Second_Try.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Map all models to SQL Server tables
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Second_Try.Models.Route> Routes { get; set; }
        public DbSet<PriceList> PriceLists { get; set; }
        public DbSet<BookingRequest> BookingRequests { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<BusSchedule> BusSchedules { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Delete Behavior to prevent Multiple Cascade Path errors in EF Core
            modelBuilder.Entity<BookingRequest>()
                .HasOne(br => br.Customer)
                .WithMany(c => c.BookingRequests)
                .HasForeignKey(br => br.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookingRequest>()
                .HasOne(br => br.Route)
                .WithMany(r => r.BookingRequests)
                .HasForeignKey(br => br.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Bus)
                .WithMany(bus => bus.Bookings)
                .HasForeignKey(b => b.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.BookingRequest)
                .WithMany()
                .HasForeignKey(r => r.BookingRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
