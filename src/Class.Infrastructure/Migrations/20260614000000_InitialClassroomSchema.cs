using System;
using Class.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Class.Infrastructure.Migrations;

[DbContext(typeof(ClassDbContext))]
[Migration("20260614000000_InitialClassroomSchema")]
public partial class InitialClassroomSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "classroom");

        migrationBuilder.CreateTable(
            name: "classes",
            schema: "classroom",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_classes", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_events",
            schema: "classroom",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                event_version = table.Column<int>(type: "integer", nullable: false),
                routing_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                payload_json = table.Column<string>(type: "jsonb", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                publish_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                last_error = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_events", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "class_students",
            schema: "classroom",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                class_id = table.Column<Guid>(type: "uuid", nullable: false),
                student_id = table.Column<Guid>(type: "uuid", nullable: false),
                joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_class_students", x => x.id);
                table.ForeignKey(
                    name: "fk_class_students_classes_class_id",
                    column: x => x.class_id,
                    principalSchema: "classroom",
                    principalTable: "classes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "labs",
            schema: "classroom",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                class_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                requirement_object_key = table.Column<string>(type: "text", nullable: false),
                collection_object_key = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                deadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                assets_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_labs", x => x.id);
                table.CheckConstraint("ck_labs_status", "status IN ('pending_assets', 'active', 'archived')");
                table.ForeignKey(
                    name: "fk_labs_classes_class_id",
                    column: x => x.class_id,
                    principalSchema: "classroom",
                    principalTable: "classes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_class_students_class_id",
            schema: "classroom",
            table: "class_students",
            column: "class_id");

        migrationBuilder.CreateIndex(
            name: "ix_class_students_student_id",
            schema: "classroom",
            table: "class_students",
            column: "student_id");

        migrationBuilder.CreateIndex(
            name: "ux_class_students_class_student",
            schema: "classroom",
            table: "class_students",
            columns: new[] { "class_id", "student_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_classes_created_by",
            schema: "classroom",
            table: "classes",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "ix_classes_name",
            schema: "classroom",
            table: "classes",
            column: "name");

        migrationBuilder.Sql("CREATE INDEX ix_classes_name_lower ON classroom.classes (lower(name));");

        migrationBuilder.CreateIndex(
            name: "ix_labs_class_id",
            schema: "classroom",
            table: "labs",
            column: "class_id");

        migrationBuilder.CreateIndex(
            name: "ix_labs_created_by",
            schema: "classroom",
            table: "labs",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "ix_labs_deadline",
            schema: "classroom",
            table: "labs",
            column: "deadline");

        migrationBuilder.CreateIndex(
            name: "ix_labs_status",
            schema: "classroom",
            table: "labs",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_outbox_events_event_type",
            schema: "classroom",
            table: "outbox_events",
            column: "event_type");

        migrationBuilder.CreateIndex(
            name: "ix_outbox_events_occurred_at",
            schema: "classroom",
            table: "outbox_events",
            column: "occurred_at");

        migrationBuilder.CreateIndex(
            name: "ix_outbox_events_published_at",
            schema: "classroom",
            table: "outbox_events",
            column: "published_at");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS classroom.ix_classes_name_lower;");
        migrationBuilder.DropTable(name: "class_students", schema: "classroom");
        migrationBuilder.DropTable(name: "labs", schema: "classroom");
        migrationBuilder.DropTable(name: "outbox_events", schema: "classroom");
        migrationBuilder.DropTable(name: "classes", schema: "classroom");
    }
}
