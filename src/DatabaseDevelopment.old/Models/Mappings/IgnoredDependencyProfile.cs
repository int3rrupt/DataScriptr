using AutoMapper;
using DatabaseDevelopment.Enums;
using DatabaseDevelopment.Models.Schema;

namespace DatabaseDevelopment.Models.Mappings
{
    public class IgnoredDependencyProfile : Profile
    {
        public IgnoredDependencyProfile()
        {
            IMappingExpression<System.Data.DataRow, IgnoredDependency> mappingExpression;

            mappingExpression = CreateMap<System.Data.DataRow, IgnoredDependency>();
            mappingExpression.ForMember(ignoredDependency => ignoredDependency.DependencyName, cfgExp => cfgExp.MapFrom(dataRow => dataRow["DependencyName"]));
            mappingExpression.ForMember(ignoredDependency => ignoredDependency.ParentTableSchema, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ParentTableSchema"]));
            mappingExpression.ForMember(ignoredDependency => ignoredDependency.ParentTableName, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ParentTableName"]));
            mappingExpression.ForMember(ignoredDependency => ignoredDependency.ParentColumnName, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ParentColumnName"]));
            mappingExpression.ForMember(ignoredDependency => ignoredDependency.ChildTableSchema, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ChildTableSchema"]));
            mappingExpression.ForMember(ignoredDependency => ignoredDependency.ChildTableName, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ChildTableName"]));
            mappingExpression.ForMember(ignoredDependency => ignoredDependency.ChildColumnName, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ChildColumnName"]));
            mappingExpression.ForMember(ignoredDependency => ignoredDependency.TableDependencyType, cfgExp => cfgExp.MapFrom(dataRow => (TableDependencyType)dataRow["TableDependencyTypeId"]));
        }
    }
}