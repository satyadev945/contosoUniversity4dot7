namespace ContosoUniversity.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "Course",
                c => new
                    {
                        CourseID = c.Int(nullable: false),
                        Title = c.String(),
                        Credits = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CourseID);

            CreateTable(
                "Enrollment",
                c => new
                    {
                        EnrollmentID = c.Int(nullable: false, identity: true),
                        CourseID = c.Int(nullable: false),
                        StudentID = c.Int(nullable: false),
                        Grade = c.Int(),
                    })
                .PrimaryKey(t => t.EnrollmentID)
                .ForeignKey("Course", t => t.CourseID, cascadeDelete: true)
                .ForeignKey("Student", t => t.StudentID, cascadeDelete: true)
                .Index(t => t.CourseID)
                .Index(t => t.StudentID);

            CreateTable(
                "Student",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        LastName = c.String(),
                        FirstMidName = c.String(),
                        EnrollmentDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);

        }

        public override void Down()
        {
            DropForeignKey("Enrollment", "StudentID", "Student");
            DropForeignKey("Enrollment", "CourseID", "Course");
            DropIndex("Enrollment", new[] { "StudentID" });
            DropIndex("Enrollment", new[] { "CourseID" });
            DropTable("Student");
            DropTable("Enrollment");
            DropTable("Course");
        }
    }
}
