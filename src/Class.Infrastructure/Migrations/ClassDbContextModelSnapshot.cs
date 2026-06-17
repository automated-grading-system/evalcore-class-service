using System;
using Class.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Class.Infrastructure.Migrations;

[DbContext(typeof(ClassDbContext))]
partial class ClassDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("classroom");
        modelBuilder.HasAnnotation("ProductVersion", "8.0.11");

        modelBuilder.Entity("Class.Domain.Entities.ClassStudent", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id").HasColumnType("uuid");
            b.Property<Guid>("ClassId").HasColumnName("class_id").HasColumnType("uuid");
            b.Property<DateTimeOffset>("JoinedAt").HasColumnName("joined_at").HasColumnType("timestamp with time zone");
            b.Property<Guid>("StudentId").HasColumnName("student_id").HasColumnType("uuid");

            b.HasKey("Id");
            b.HasIndex("ClassId").HasDatabaseName("ix_class_students_class_id");
            b.HasIndex("StudentId").HasDatabaseName("ix_class_students_student_id");
            b.HasIndex("ClassId", "StudentId").IsUnique().HasDatabaseName("ux_class_students_class_student");
            b.ToTable("class_students", "classroom");
        });

        modelBuilder.Entity("Class.Domain.Entities.ClassroomClass", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at").HasColumnType("timestamp with time zone");
            b.Property<Guid>("CreatedBy").HasColumnName("created_by").HasColumnType("uuid");
            b.Property<string>("Description").HasColumnName("description").HasColumnType("text");
            b.Property<string>("Name").IsRequired().HasMaxLength(150).HasColumnName("name").HasColumnType("character varying(150)");
            b.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at").HasColumnType("timestamp with time zone");

            b.HasKey("Id");
            b.HasIndex("CreatedBy").HasDatabaseName("ix_classes_created_by");
            b.HasIndex("Name").HasDatabaseName("ix_classes_name");
            b.ToTable("classes", "classroom");
        });

        modelBuilder.Entity("Class.Domain.Entities.Lab", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id").HasColumnType("uuid");
            b.Property<DateTimeOffset?>("AssetsCompletedAt").HasColumnName("assets_completed_at").HasColumnType("timestamp with time zone");
            b.Property<Guid>("ClassId").HasColumnName("class_id").HasColumnType("uuid");
            b.Property<string>("CollectionObjectKey").IsRequired().HasColumnName("collection_object_key").HasColumnType("text");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at").HasColumnType("timestamp with time zone");
            b.Property<Guid>("CreatedBy").HasColumnName("created_by").HasColumnType("uuid");
            b.Property<DateTimeOffset>("Deadline").HasColumnName("deadline").HasColumnType("timestamp with time zone");
            b.Property<string>("Description").HasColumnName("description").HasColumnType("text");
            b.Property<string>("RequirementObjectKey").IsRequired().HasColumnName("requirement_object_key").HasColumnType("text");
            b.Property<string>("Status").IsRequired().HasColumnName("status").HasColumnType("text");
            b.Property<string>("Title").IsRequired().HasMaxLength(200).HasColumnName("title").HasColumnType("character varying(200)");
            b.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at").HasColumnType("timestamp with time zone");

            b.HasKey("Id");
            b.HasIndex("ClassId").HasDatabaseName("ix_labs_class_id");
            b.HasIndex("CreatedBy").HasDatabaseName("ix_labs_created_by");
            b.HasIndex("Deadline").HasDatabaseName("ix_labs_deadline");
            b.HasIndex("Status").HasDatabaseName("ix_labs_status");
            b.ToTable("labs", "classroom", table => table.HasCheckConstraint("ck_labs_status", "status IN ('pending_assets', 'active', 'archived')"));
        });

        modelBuilder.Entity("Class.Domain.Entities.OutboxEvent", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id").HasColumnType("uuid");
            b.Property<string>("EventType").IsRequired().HasMaxLength(100).HasColumnName("event_type").HasColumnType("character varying(100)");
            b.Property<int>("EventVersion").HasColumnName("event_version").HasColumnType("integer");
            b.Property<string>("LastError").HasColumnName("last_error").HasColumnType("text");
            b.Property<DateTimeOffset>("OccurredAt").HasColumnName("occurred_at").HasColumnType("timestamp with time zone");
            b.Property<string>("PayloadJson").IsRequired().HasColumnName("payload_json").HasColumnType("jsonb");
            b.Property<DateTimeOffset?>("PublishedAt").HasColumnName("published_at").HasColumnType("timestamp with time zone");
            b.Property<int>("PublishAttempts").HasColumnName("publish_attempts").HasColumnType("integer").HasDefaultValue(0);
            b.Property<string>("RoutingKey").IsRequired().HasMaxLength(200).HasColumnName("routing_key").HasColumnType("character varying(200)");

            b.HasKey("Id");
            b.HasIndex("EventType").HasDatabaseName("ix_outbox_events_event_type");
            b.HasIndex("OccurredAt").HasDatabaseName("ix_outbox_events_occurred_at");
            b.HasIndex("PublishedAt").HasDatabaseName("ix_outbox_events_published_at");
            b.ToTable("outbox_events", "classroom");
        });

        modelBuilder.Entity("Class.Domain.Entities.ClassStudent", b =>
        {
            b.HasOne("Class.Domain.Entities.ClassroomClass")
                .WithMany()
                .HasForeignKey("ClassId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Class.Domain.Entities.Lab", b =>
        {
            b.HasOne("Class.Domain.Entities.ClassroomClass")
                .WithMany()
                .HasForeignKey("ClassId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }
}
