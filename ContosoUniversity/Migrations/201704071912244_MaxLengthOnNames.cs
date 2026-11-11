namespace ContosoUniversity.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class MaxLengthOnNames : DbMigration
    {
        public override void Up()
        {
            AlterColumn("Student", "LastName", c => c.String(maxLength: 50));
            AlterColumn("Student", "FirstMidName", c => c.String(maxLength: 50));
        }

        public override void Down()
        {
            AlterColumn("Student", "FirstMidName", c => c.String());
            AlterColumn("Student", "LastName", c => c.String());
        }
    }
}
