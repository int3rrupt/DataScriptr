using DatabaseDevelopment.Standard.Databases;
using Moq;
using System;
using System.Data;
using System.IO;
using System.Reflection;
using Xunit;

namespace DatabaseDevelopment.Tests
{
    public class DataReaderTests
    {
        [Fact]
        public void Test1()
        {
            //var tmp = new SqlDatabase("Server=(localdb)\\ProjectsV13;Initial Catalog=ErisDB;Integrated Security=True;MultipleActiveResultSets=False;Connection Timeout=30;");
            //var pk = tmp.GetPrimaryKeyConstraintsFromDatabase();
            //pk.WriteXml(@"\Users\e548314\Desktop\file.xml");



            var mock = new Mock<ISqlDatabase>();
            var dataTable = new DataSet();
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DatabaseDevelopment.Tests.Data.PrimaryKeyConstraints.xml"))
            {
                dataTable.ReadXml(stream);
            }
            mock.Setup(x => x.GetPrimaryKeyConstraintsFromDatabase()).Returns(dataTable.Tables[0]);
            var dataReader = new DataReader(mock.Object);
            var test = dataReader.GetPrimaryKeyConstraintsFromDatabase();
        }
    }
}