namespace ContosoUniversity.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class ComplexDataModel : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "InstructorCourse", newName: "CourseInstructor");
            RenameColumn(table: "CourseInstructor", name: "Instructor_ID", newName: "InstructorID");
            RenameColumn(table: "CourseInstructor", name: "Course_CourseID", newName: "CourseID");
            RenameIndex(table: "CourseInstructor", name: "IX_Course_CourseID", newName: "IX_CourseID");
            RenameIndex(table: "CourseInstructor", name: "IX_Instructor_ID", newName: "IX_InstructorID");
            DropPrimaryKey("CourseInstructor");
            AddPrimaryKey("CourseInstructor", new[] { "CourseID", "InstructorID" });
        }

        public override void Down()
        {
            DropPrimaryKey("CourseInstructor");
            AddPrimaryKey("CourseInstructor", new[] { "Instructor_ID", "Course_CourseID" });
            RenameIndex(table: "CourseInstructor", name: "IX_InstructorID", newName: "IX_Instructor_ID");
            RenameIndex(table: "CourseInstructor", name: "IX_CourseID", newName: "IX_Course_CourseID");
            RenameColumn(table: "CourseInstructor", name: "CourseID", newName: "Course_CourseID");
            RenameColumn(table: "CourseInstructor", name: "InstructorID", newName: "Instructor_ID");
            RenameTable(name: "CourseInstructor", newName: "InstructorCourse");
        }
    }
}
