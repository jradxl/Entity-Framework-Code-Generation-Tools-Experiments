 
using System;
using System.Data;
namespace dbSchema{
    public static class Tables
        {        
 
            public static class Addresses {
                public static string Name = "Addresses";
                public static class Columns {
                  public static Column AddressID= new Column("AddressID",false,"int",null);
                  public static Column Address1= new Column("Address1",false,"nvarchar",-1);
                  public static Column Address2= new Column("Address2",false,"nvarchar",-1);
                  public static Column Address3= new Column("Address3",false,"nvarchar",-1);
                  public static Column City= new Column("City",false,"nvarchar",-1);
                  public static Column Country= new Column("Country",false,"nvarchar",-1);
                  public static Column Postcode= new Column("Postcode",false,"nvarchar",-1);
            
                }
            }
 
            public static class People {
                public static string Name = "People";
                public static class Columns {
                  public static Column PersonID= new Column("PersonID",false,"int",null);
                  public static Column Lastname= new Column("Lastname",false,"nvarchar",-1);
                  public static Column Firstname= new Column("Firstname",false,"nvarchar",-1);
            
                }
            }
 
            public static class PersonAddress {
                public static string Name = "PersonAddress";
                public static class Columns {
                  public static Column People_PersonID= new Column("People_PersonID",false,"int",null);
                  public static Column Addresses_AddressID= new Column("Addresses_AddressID",false,"int",null);
            
                }
            }
 
            public static class PersonInfoes {
                public static string Name = "PersonInfoes";
                public static class Columns {
                  public static Column PersonID= new Column("PersonID",false,"int",null);
                  public static Column Data1= new Column("Data1",false,"nvarchar",-1);
                  public static Column Data2= new Column("Data2",false,"nvarchar",-1);
                  public static Column Person_PersonID= new Column("Person_PersonID",false,"int",null);
            
                }
            }
 
            public static class Phones {
                public static string Name = "Phones";
                public static class Columns {
                  public static Column PhoneID= new Column("PhoneID",false,"int",null);
                  public static Column PhoneNumber= new Column("PhoneNumber",false,"nvarchar",-1);
                  public static Column PersonID= new Column("PersonID",false,"int",null);
            
                }
            }
}
 
    public class Column {
        public Column(string columnName, bool isNullable, string dbType, int? maxlength)
            {
                IsNullable = isNullable;
                SqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), dbType, true);
                ColumnName = columnName;
                MaxLength = maxlength;
            }
 
        public bool IsNullable { get; set; }
 
        public SqlDbType SqlDbType { get; set; }
 
        public int? MaxLength { get; set; }
 
        public string ColumnName { get; set; }
 
        public override string ToString()
            {
                return ColumnName;
            }
    }
}
 
