namespace ContosoUniversity.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class Additions : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "Department",
                c => new
                    {
                        DepartmentID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 50),
                        Budget = c.Decimal(nullable: false, storeType: "money"),
                        StartDate = c.DateTime(nullable: false),
                        InstructorID = c.Int(),
                    })
                .PrimaryKey(t => t.DepartmentID)
                .ForeignKey("Instructor", t => t.InstructorID)
                .Index(t => t.InstructorID);

            CreateTable(
                "Instructor",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        LastName = c.String(nullable: false, maxLength: 50),
                        FirstName = c.String(nullable: false, maxLength: 50),
                        HireDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);

            CreateTable(
                "OfficeAssignment",
                c => new
                    {
                        InstructorID = c.Int(nullable: false),
                        Location = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.InstructorID)
                .ForeignKey("Instructor", t => t.InstructorID)
                .Index(t => t.InstructorID);

            CreateTable(
                "InstructorCourse",
                c => new
                    {
                        Instructor_ID = c.Int(nullable: false),
                        Course_CourseID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Instructor_ID, t.Course_CourseID })
                .ForeignKey("Instructor", t => t.Instructor_ID, cascadeDelete: true)
                .ForeignKey("Course", t => t.Course_CourseID, cascadeDelete: true)
                .Index(t => t.Instructor_ID)
                .Index(t => t.Course_CourseID);

            AddColumn("Course", "DepartmentID", c => c.Int(nullable: false));
            AlterColumn("Course", "Title", c => c.String(maxLength: 50));
            AlterColumn("Student", "LastName", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("Student", "FirstName", c => c.String(nullable: false, maxLength: 50));
            CreateIndex("Course", "DepartmentID");
            AddForeignKey("Course", "DepartmentID", "Department", "DepartmentID", cascadeDelete: true);
        }

        public override void Down()
        {
            DropForeignKey("Course", "DepartmentID", "Department");
            DropForeignKey("Department", "InstructorID", "Instructor");
            DropForeignKey("OfficeAssignment", "InstructorID", "Instructor");
            DropForeignKey("InstructorCourse", "Course_CourseID", "Course");
            DropForeignKey("InstructorCourse", "Instructor_ID", "Instructor");
            DropIndex("InstructorCourse", new[] { "Course_CourseID" });
            DropIndex("InstructorCourse", new[] { "Instructor_ID" });
            DropIndex("OfficeAssignment", new[] { "InstructorID" });
            DropIndex("Department", new[] { "InstructorID" });
            DropIndex("Course", new[] { "DepartmentID" });
            AlterColumn("Student", "FirstName", c => c.String(maxLength: 50));
            AlterColumn("Student", "LastName", c => c.String(maxLength: 50));
            AlterColumn("Course", "Title", c => c.String());
            DropColumn("Course", "DepartmentID");
            DropTable("InstructorCourse");
            DropTable("OfficeAssignment");
            DropTable("Instructor");
            DropTable("Department");
        }
    }
}
