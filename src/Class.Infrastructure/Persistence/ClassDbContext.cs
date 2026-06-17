using Class.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Class.Infrastructure.Persistence;

public sealed class ClassDbContext : DbContext
{
    public ClassDbContext(DbContextOptions<ClassDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClassroomClass> Classes => Set<ClassroomClass>();

    public DbSet<ClassStudent> ClassStudents => Set<ClassStudent>();

    public DbSet<Lab> Labs => Set<Lab>();

    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("classroom");

        modelBuilder.Entity<ClassroomClass>(entity =>
        {
            entity.ToTable("classes");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasIndex(x => x.CreatedBy).HasDatabaseName("ix_classes_created_by");
            entity.HasIndex(x => x.Name).HasDatabaseName("ix_classes_name");
        });

        modelBuilder.Entity<ClassStudent>(entity =>
        {
            entity.ToTable("class_students");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ClassId).HasColumnName("class_id").IsRequired();
            entity.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
            entity.Property(x => x.JoinedAt).HasColumnName("joined_at").IsRequired();

            entity.HasIndex(x => x.ClassId).HasDatabaseName("ix_class_students_class_id");
            entity.HasIndex(x => x.StudentId).HasDatabaseName("ix_class_students_student_id");
            entity.HasIndex(x => new { x.ClassId, x.StudentId }).IsUnique().HasDatabaseName("ux_class_students_class_student");

            entity
                .HasOne<ClassroomClass>()
                .WithMany()
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lab>(entity =>
        {
            entity.ToTable("labs", table => table.HasCheckConstraint("ck_labs_status", "status IN ('pending_assets', 'active', 'archived')"));
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ClassId).HasColumnName("class_id").IsRequired();
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.RequirementObjectKey).HasColumnName("requirement_object_key").IsRequired();
            entity.Property(x => x.CollectionObjectKey).HasColumnName("collection_object_key").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").IsRequired();
            entity.Property(x => x.Deadline).HasColumnName("deadline").IsRequired();
            entity.Property(x => x.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.AssetsCompletedAt).HasColumnName("assets_completed_at");

            entity.HasIndex(x => x.ClassId).HasDatabaseName("ix_labs_class_id");
            entity.HasIndex(x => x.CreatedBy).HasDatabaseName("ix_labs_created_by");
            entity.HasIndex(x => x.Deadline).HasDatabaseName("ix_labs_deadline");
            entity.HasIndex(x => x.Status).HasDatabaseName("ix_labs_status");

            entity
                .HasOne<ClassroomClass>()
                .WithMany()
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("outbox_events");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
            entity.Property(x => x.EventVersion).HasColumnName("event_version").IsRequired();
            entity.Property(x => x.RoutingKey).HasColumnName("routing_key").HasMaxLength(200).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
            entity.Property(x => x.PublishedAt).HasColumnName("published_at");
            entity.Property(x => x.PublishAttempts).HasColumnName("publish_attempts").HasDefaultValue(0).IsRequired();
            entity.Property(x => x.LastError).HasColumnName("last_error");

            entity.HasIndex(x => x.PublishedAt).HasDatabaseName("ix_outbox_events_published_at");
            entity.HasIndex(x => x.EventType).HasDatabaseName("ix_outbox_events_event_type");
            entity.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_outbox_events_occurred_at");
        });
    }
}
