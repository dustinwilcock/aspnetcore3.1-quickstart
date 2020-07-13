using System;
using Microsoft.EntityFrameworkCore;

namespace QuickStart.Models
{
    public class RosterDbContext : DbContext
    {
        public RosterDbContext(DbContextOptions<RosterDbContext> options) : base(options) { }

        public DbSet<School> School { get; set; }
        public DbSet<Teacher> Teacher { get; set; }
        public DbSet<Class> Class { get; set; }
        public DbSet<Student> Student { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<School>(school =>
            {
                school.HasKey(s => s.Id);
                school.Property(s => s.Name).IsRequired();
                school.Property(s => s.City).IsRequired();
                school.Property(s => s.State).IsRequired();
                school.HasMany(s => s.Teachers).WithOne(t => t.School);
            });

            modelBuilder.Entity<Teacher>(teacher =>
            {
                teacher.HasKey(t => t.Id);
                teacher.Property(t => t.Name).IsRequired();
                teacher.HasOne(t => t.School).WithMany(s => s.Teachers);
                teacher.HasMany(t => t.Classes).WithOne(c => c.Teacher);
            });

            modelBuilder.Entity<Class>(@class =>
            {
                @class.HasKey(c => c.Id);
                @class.Property(c => c.Name).IsRequired();
                @class.HasOne(c => c.Teacher).WithMany(t => t.Classes);
                @class.HasMany(c => c.Students).WithOne(s => s.Class);
            });

            modelBuilder.Entity<Student>(student =>
            {
                student.HasKey(s => s.Id);
                student.Property(s => s.Name).IsRequired();
                student.HasOne(s => s.Class).WithMany(c => c.Students);
            });
        }
    }
}
